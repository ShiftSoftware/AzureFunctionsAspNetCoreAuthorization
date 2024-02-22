
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
            Username = "Test",
            Password = "Test",
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
        var azureFunctionPath = Environment.ProcessPath!.Substring(0, Environment.ProcessPath!.IndexOf("Tests\\")) + @"AzureFunctions.Sample\bin\Debug\net8.0";

        //Wrapped around using. Because the function app stays open and prevents you from building the solution if not dispossed
        await using (var app = (await TemporaryAzureFunctionsApplication.StartNewAsync(new DirectoryInfo(azureFunctionPath))))
        {
            var httpClient = new HttpClient();

            var unauthenticatedResponse = await httpClient.GetAsync("http://localhost:7050/api/hello");

            Assert.Equal(401, (int)unauthenticatedResponse.StatusCode);

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

            var authenticatedResponse = await httpClient.GetAsync("http://localhost:7050/api/hello");

            authenticatedResponse.EnsureSuccessStatusCode();
        }
    }
}