
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Sample;

[Authorize]
public class AuthorizeOnClassAndAllowAnonymous
{
    [Function("authorized-on-class")]
    public IActionResult SayHello([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }

    [Function("allow-anonymous")]
    [AllowAnonymous]
    public IActionResult AllowAnonymous([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
}