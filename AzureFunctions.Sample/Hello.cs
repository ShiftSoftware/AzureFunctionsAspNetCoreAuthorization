using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Sample;

public class Hello
{
    private readonly ILogger<Hello> _logger;

    public Hello(ILogger<Hello> logger)
    {
        _logger = logger;
    }

    [Function("hello")]
    [AllowAnonymous]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        return new OkObjectResult("Hello");
    }
}