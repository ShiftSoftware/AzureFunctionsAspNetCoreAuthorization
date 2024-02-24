using System.Security.Claims;

namespace Microsoft.Azure.Functions.Worker.Http;

public static class HttpRequestDataExtensions
{
    public static ClaimsPrincipal GetUser(this HttpRequestData req)
    {
        return req.FunctionContext.GetUser();
    }
}