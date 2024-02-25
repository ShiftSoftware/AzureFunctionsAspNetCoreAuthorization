
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
    [Fact(DisplayName = "03. Use the Token to call an Azure Function Endpoint.")]
    [TestPriority(3)]
    public async Task AzureFunctionHello()
    {
        var testDirectory = Directory.GetCurrentDirectory();

        var debugAzureFunctionSampleDirectory = testDirectory.Substring(0, testDirectory.IndexOf("Tests")) + "AzureFunctions.Sample/bin/Debug/net8.0";

        var releaseAzureFunctionSampleDirectory = testDirectory.Substring(0, testDirectory.IndexOf("Tests")) + "AzureFunctions.Sample/bin/Release/net8.0";

        var azureFunctionSampleDirectory = "";

        if (Directory.Exists(releaseAzureFunctionSampleDirectory))
            azureFunctionSampleDirectory = releaseAzureFunctionSampleDirectory;
        else
            azureFunctionSampleDirectory = debugAzureFunctionSampleDirectory;

        //Wrapped around using. Because the function app stays open and prevents you from building the solution if not dispossed
        await using (var app = (await TemporaryAzureFunctionsApplication.StartNewAsync(new DirectoryInfo(azureFunctionSampleDirectory))))
        {
            var httpClient = new HttpClient();

            var unauthenticatedResponse_HttpReponseData = await httpClient.GetAsync("http://localhost:7050/api/hello-http-response-data");

            var unauthenticatedResponse_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/hello--iaction-result");

            Assert.Equal(401, (int) unauthenticatedResponse_HttpReponseData.StatusCode);

            Assert.Equal(401, (int) unauthenticatedResponse_IActionResult.StatusCode);

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

            var authenticatedResponse_HttpReponseData = await httpClient.GetAsync("http://localhost:7050/api/hello-http-response-data");

            var authenticatedResponse_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/hello--iaction-result");

            authenticatedResponse_HttpReponseData.EnsureSuccessStatusCode();
            
            authenticatedResponse_IActionResult.EnsureSuccessStatusCode();

            Assert.Equal("Hello", await authenticatedResponse_HttpReponseData.Content.ReadAsStringAsync());

            Assert.Equal("Hello", await authenticatedResponse_IActionResult.Content.ReadAsStringAsync());

            var claims_HttpReponseData = await httpClient.GetAsync("http://localhost:7050/api/claims-http-response-data");

            var claims_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/claims--iaction-result");

            var claimsDictionary_HttpReponseData = System.Text.Json.Nodes.JsonNode.Parse(await claims_HttpReponseData.Content.ReadAsStringAsync())!
                .AsObject().ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());

            var claimsDictionary_IActionResult = System.Text.Json.Nodes.JsonNode.Parse(await claims_IActionResult.Content.ReadAsStringAsync())!
                .AsObject().ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());

            Assert.Equal("Admin", claimsDictionary_HttpReponseData[ClaimTypes.NameIdentifier]);
            Assert.Equal("Kurdistan", claimsDictionary_HttpReponseData[ClaimTypes.Country]);
            Assert.Equal("a@a.a", claimsDictionary_HttpReponseData[ClaimTypes.Email]);

            Assert.Equal("Admin", claimsDictionary_IActionResult[ClaimTypes.NameIdentifier]);
            Assert.Equal("Kurdistan", claimsDictionary_IActionResult[ClaimTypes.Country]);
            Assert.Equal("a@a.a", claimsDictionary_IActionResult[ClaimTypes.Email]);


            var usaResident_HttpReponseData = await httpClient.GetAsync("http://localhost:7050/api/usa-resident-http-response-data");

            var usaResident_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/usa-resident-iaction-result");

            Assert.Equal(403, (int) usaResident_HttpReponseData.StatusCode);

            Assert.Equal(403, (int) usaResident_IActionResult.StatusCode);

            var kurdistan_HttpReponseData = await httpClient.GetAsync("http://localhost:7050/api/kurdistan-resident-http-response-data");

            var kurdistan_IActionResult = await httpClient.GetAsync("http://localhost:7050/api/kurdistan-resident-iaction-result");

            Assert.Equal(200, (int)kurdistan_HttpReponseData.StatusCode);

            Assert.Equal(200, (int)kurdistan_IActionResult.StatusCode);
        }
    }
}