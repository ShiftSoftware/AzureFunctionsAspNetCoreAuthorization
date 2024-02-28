using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;

namespace AzureFunctions.Sample;

public class Claims
{
    [Function("claims")]
    [Authorize]
    public IActionResult GetClaims([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        var user = context.GetUser();

        var claims = new Dictionary<string, string?>
        {
            [ClaimTypes.NameIdentifier] = user.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value,
            [ClaimTypes.Country] = user.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Country)?.Value,
            [ClaimTypes.Email] = user.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
        };

        return new OkObjectResult(claims);
    }
}