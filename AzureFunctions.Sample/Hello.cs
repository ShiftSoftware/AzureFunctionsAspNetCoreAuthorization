using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;
using System.Net;
using System.Security.Claims;

namespace AzureFunctions.Sample;

public class Hello
{
    private readonly ILogger<Hello> _logger;

    public Hello(ILogger<Hello> logger)
    {
        _logger = logger;
    }

    [Function("hello")]
    [Authorize]
    public async Task<HttpResponseData> RunHello([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        var user = req.GetUser();

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync(user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value);

        return response;
    }

    [Function("hello2")]
    [Authorize]
    public async Task<IActionResult> RunHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, 
        FunctionContext context)
    {
        var user = context.GetUser();
        
        return new OkObjectResult(user.Claims.FirstOrDefault(x=> x.Type== ClaimTypes.NameIdentifier).Value);
    }
}