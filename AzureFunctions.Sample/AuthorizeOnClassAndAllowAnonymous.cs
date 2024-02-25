
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;

namespace AzureFunctions.Sample;

[Authorize]
public class AuthorizeOnClassAndAllowAnonymous
{
    [Function("authorize-on-class-http-response-data")]
    public async Task<HttpResponseData> SayHello([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Hello");

        return response;
    }

    [Function("authorize-on-class-iaction-result")]
    public IActionResult SayHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }

    [Function("allow-anonymous-http-response-data")]
    [AllowAnonymous]
    public async Task<HttpResponseData> AllowAnonymous([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Hello");

        return response;
    }

    [Function("allow-anonymous-iaction-result")]
    [AllowAnonymous]
    public IActionResult AllowAnonymous2([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
}