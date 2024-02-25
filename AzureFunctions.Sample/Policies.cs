
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;

namespace AzureFunctions.Sample;

public class Policies
{
    [Function("usa-resident-http-response-data")]
    [Authorize(Policy = "USA-Resident")]
    public HttpResponseData UsaResident1([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        return response;
    }

    [Function("usa-resident-iaction-result")]
    [Authorize(Policy = "USA-Resident")]
    public IActionResult UsaResident2([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, FunctionContext context)
    {
        return new OkResult();
    }

    [Function("kurdistan-resident-http-response-data")]
    [Authorize(Policy = "Kurdistan-Resident")]
    public HttpResponseData KurdistanResident1([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        return response;
    }

    [Function("kurdistan-resident-iaction-result")]
    [Authorize(Policy = "Kurdistan-Resident")]
    public IActionResult KurdistanResident2([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, FunctionContext context)
    {
        return new OkResult();
    }
}