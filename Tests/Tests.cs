
using System.Security.Claims;
using System.Text;
using Xunit.Abstractions;

namespace Tests;

[Collection("API Collection")]
[TestCaseOrderer($"{(nameof(Tests))}.{(nameof(PriorityOrderer))}", nameof(Tests))]
public class Tests
{
    private readonly HttpClient aspNetCoreClient;
    private readonly ITestOutputHelper output;
    private static string? Token;

    public Tests(AspNetCoreApplicationFactory factory, ITestOutputHelper output)
    {
        this.aspNetCoreClient = factory.CreateClient();
        this.output = output;
    }

    [Fact(DisplayName = "01. Get a Token from AspNetCore")]
    [TestPriority(1)]
    public async Task GetToken()
    {
        var login = new LoginDTO
        {
            Username = "Admin",
            Password = "Admin",
            Claims = new()
            {
                [ClaimTypes.Country] = "Kurdistan",
                [ClaimTypes.Email] = "a@a.a",
            }
        };

        var response = await aspNetCoreClient.PostAsync("login", new StringContent(System.Text.Json.JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        Token = await response.Content.ReadAsStringAsync();
    }

    [Fact(DisplayName = "02. Use the Token to call another AspNetCore endpoint")]
    [TestPriority(2)]
    public async Task UseToken()
    {
        this.aspNetCoreClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

        var response = await this.aspNetCoreClient.GetAsync("hello");

        response.EnsureSuccessStatusCode();
    }

    //https://www.codit.eu/blog/locally-integration-testing-azure-functions-applications/?country_sel=be
    [Fact(DisplayName = "03. Use the Token With Functions Registered by ConfigureFunctionsWebApplication()")]
    [TestPriority(3)]
    public async Task TestFunctionsWithConfigureFunctionsWebApplication()
    {
        var testDirectory = Directory.GetCurrentDirectory();

        var debugAzureFunctionSampleDirectory = testDirectory.Substring(0, testDirectory.IndexOf("Tests")) + "AzureFunctions.Sample/bin/Debug/net9.0";

        var releaseAzureFunctionSampleDirectory = testDirectory.Substring(0, testDirectory.IndexOf("Tests")) + "AzureFunctions.Sample/bin/Release/net9.0";

        var azureFunctionSampleDirectory = "";

        if (Directory.Exists(releaseAzureFunctionSampleDirectory))
            azureFunctionSampleDirectory = releaseAzureFunctionSampleDirectory;
        else
            azureFunctionSampleDirectory = debugAzureFunctionSampleDirectory;

        //Wrapped around using. Because the function app stays open and prevents you from building the solution if not dispossed
        await using (var app = (await TemporaryAzureFunctionsApplication.StartNewAsync(new DirectoryInfo(azureFunctionSampleDirectory))))
        {
            var httpClient = new HttpClient();

            var unauthenticatedResponse = await httpClient.GetAsync("http://localhost:7050/api/hello");

            var unauthenticatedResponseOnClass = await httpClient.GetAsync("http://localhost:7050/api/authorized-on-class");

            var unauthenticatedAnonymousResponse = await httpClient.GetAsync("http://localhost:7050/api/allow-anonymous");

            Assert.Equal(401, (int)unauthenticatedResponse.StatusCode);

            Assert.Equal(401, (int)unauthenticatedResponseOnClass.StatusCode);

            Assert.Equal(200, (int)unauthenticatedAnonymousResponse.StatusCode);

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

            var authenticatedResponse = await httpClient.GetAsync("http://localhost:7050/api/hello");

            var authenticatedResponseOnClass = await httpClient.GetAsync("http://localhost:7050/api/authorized-on-class");

            var authenticatedAnonymousResponse = await httpClient.GetAsync("http://localhost:7050/api/allow-anonymous");

            Assert.Equal(200, (int)authenticatedResponse.StatusCode);
            Assert.Equal(200, (int)authenticatedResponseOnClass.StatusCode);
            Assert.Equal(200, (int)authenticatedAnonymousResponse.StatusCode);

            Assert.Equal("Hello", await authenticatedResponse.Content.ReadAsStringAsync());
            Assert.Equal("Hello", await authenticatedResponseOnClass.Content.ReadAsStringAsync());
            Assert.Equal("Hello", await authenticatedAnonymousResponse.Content.ReadAsStringAsync());

            var claimsResponse = await httpClient.GetAsync("http://localhost:7050/api/claims");

            var claimsDictionary_IActionResult = System.Text.Json.Nodes.JsonNode.Parse(await claimsResponse.Content.ReadAsStringAsync())!
                .AsObject().ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());

            Assert.Equal("Admin", claimsDictionary_IActionResult[ClaimTypes.NameIdentifier]);
            Assert.Equal("Kurdistan", claimsDictionary_IActionResult[ClaimTypes.Country]);
            Assert.Equal("a@a.a", claimsDictionary_IActionResult[ClaimTypes.Email]);

            var usaResident = await httpClient.GetAsync("http://localhost:7050/api/usa-resident");

            Assert.Equal(403, (int)usaResident.StatusCode);

            var kurdistan_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/kurdistan-resident");

            Assert.Equal(200, (int)kurdistan_IActionResult.StatusCode);
        }
    }
}