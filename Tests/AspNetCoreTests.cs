
using System.Text;
using Xunit.Abstractions;

namespace Tests;

[Collection("API Collection")]
[TestCaseOrderer($"{nameof(Tests)}.{nameof(PriorityOrderer)}", nameof(Tests))]
public class AspNetCoreTests
{
    private readonly HttpClient aspNetCoreClient ;
    private readonly ITestOutputHelper output;
    private static string? Token;

    public AspNetCoreTests(AspNetCoreApplication factory, ITestOutputHelper output)
    {
        this.aspNetCoreClient = factory.CreateClient();
        this.output = output;
    }

    [Fact(DisplayName = "Get a Token from AspNetCore")]
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

    [Fact(DisplayName = "Use the Token to call another AspNetCore endpoint")]
    [TestPriority(2)]
    public async Task UseToken()
    {
        this.aspNetCoreClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");

        var response = await this.aspNetCoreClient.GetAsync("hello");

        response.EnsureSuccessStatusCode();
    }
}