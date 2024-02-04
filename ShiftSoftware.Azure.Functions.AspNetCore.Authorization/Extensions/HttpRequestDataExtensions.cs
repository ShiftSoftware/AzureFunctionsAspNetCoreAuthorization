using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;

public static class HttpRequestDataExtensions
{
    public static ClaimsPrincipal GetUser(this HttpRequestData req)
    {
        if(req.FunctionContext.Items["User"] is not null)
            return req.FunctionContext.Items["User"] as ClaimsPrincipal;
        else
            return new ClaimsPrincipal();
    }
}
