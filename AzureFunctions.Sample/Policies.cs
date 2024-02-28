
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Sample;

public class Policies
{
    [Function("usa-resident")]
    [Authorize(Policy = "USA-Resident")]
    public IActionResult UsaResident([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkResult();
    }

    [Function("kurdistan-resident")]
    [Authorize(Policy = "Kurdistan-Resident")]
    public IActionResult KurdistanResident([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkResult();
    }
}