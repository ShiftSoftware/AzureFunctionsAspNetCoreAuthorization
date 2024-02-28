# ShiftSoftware.Azure.Functions.AspNetCore.Authorization

Adds basic AspNetCore Authorization Middleware capabilities to Azure Functions. (isolated worker process only).

## Purpose of the Pakcage
Users holding a valid Jwt Bearer token that's issued on an AspNetCore app can call Http Triggered Azure Functions with the same token using [Authorize] attribute.

## Installation (Nuget)
Install the [`ShiftSoftware.Azure.Functions.AspNetCore.Authorization`](https://www.nuget.org/packages/ShiftSoftware.Azure.Functions.AspNetCore.Authorization) package

## Registration (Program.cs)
The ``AddAuthentication`` and ``AddJwtBearer`` can be used which should be familiar to AspNetCore developers.  

In addition to these, Policies can be added using ``AddAuthorization``

```C#
    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication(x =>
        {
            x.AddAuthentication()
            .AddJwtBearer(new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = "Issuer",
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("A secure key that's shared between AspNetCore and Azure Functions")),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            });
        })
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("USA-Resident", policy => policy.RequireClaim(ClaimTypes.Country, "USA"));
                options.AddPolicy("Kurdistan-Resident", policy => policy.RequireClaim(ClaimTypes.Country, "Kurdistan"));
            });
        })
    .Build();

    host.Run();
```

**Note:** We're developing and testing this package for `ASP.NET Core integration model` only. which is activated by **`ConfigureFunctionsWebApplication()`**.

We're not supporting the `built-in model` which is activated using **`ConfigureFunctionsWorkerDefaults()`** (Although some features might still work).

More information about HttpTrigger models is available in [this Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#http-trigger) articel.

## Using [Authorize] on Functions 
After registering the Middleware, the **`[Authorize]`** works in a similar manner as it does in AspNetCore applications. Providing that you use **``AuthorizationLevel.Anonymous``** on the HttpTrigger attribute.

```C#
    [Function("hello")]
    [Authorize]
    public IActionResult SayHello([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
```


<br/>


## Authorize multiple functions and Using [AllowAnonymous]
You can put the **`[Authorize]`** attribute on a calss that contains multiple functions.  
And then put **`[AllowAnonymous]`** on functions that need to be anonymous.

```C#
[Authorize]
public class AuthorizeOnClassAndAllowAnonymous
{
    [Function("authorized-on-class")]
    public IActionResult SayHello([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }

    [Function("allow-anonymous")]
    [AllowAnonymous]
    public IActionResult AllowAnonymous([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
}
```

## Authorize with Policies

```C#
    [Function("kurdistan-resident")]
    [Authorize(Policy = "Kurdistan-Resident")]
    public IActionResult KurdistanResident([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkResult();
    }
```

## Getting Claims
There's an extention method on ``FunctionContext`` called ``GetUser()`` that returns the ClaimsPrincipal.

```C#
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
```


[comment]: <> (Advanced Usage: Writing Custom Attributes on [Authorize])
