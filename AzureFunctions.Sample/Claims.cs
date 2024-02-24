using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Security.Claims;

namespace AzureFunctions.Sample;

public class Claims
{
    [Function("claims-http-response-data")]
    [Authorize]
    public async Task<HttpResponseData> RunHello([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        var user = req.GetUser()!;

        var claims = new
        {
            NameIdentifier = user.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
            Country = user.Claims?.FirstOrDefault(x => x.Type == "Country")?.Value,
            Email = user.Claims?.FirstOrDefault(x => x.Type == "Email")?.Value,
        };

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(claims);

        return response;
    }

    [Function("claims--iaction-result")]
    [Authorize]
    public IActionResult RunHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, FunctionContext context)
    {
        var user = context.GetUser();

        var claims = new
        {
            NameIdentifier = user.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
            Country = user.Claims?.FirstOrDefault(x => x.Type == "Country")?.Value,
            Email = user.Claims?.FirstOrDefault(x => x.Type == "Email")?.Value,
        };

        return new OkObjectResult(claims);
    }
}