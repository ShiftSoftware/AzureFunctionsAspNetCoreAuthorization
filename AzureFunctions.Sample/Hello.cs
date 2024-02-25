using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace AzureFunctions.Sample;

public class Hello
{
    [Function("hello-http-response-data")]
    [Authorize]
    public async Task<HttpResponseData> SayHello([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Hello");

        return response;
    }

    [Function("hello-iaction-result")]
    [Authorize]
    public IActionResult SayHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
}