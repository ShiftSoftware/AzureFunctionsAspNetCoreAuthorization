# ShiftSoftware.Azure.Functions.AspNetCore.Authorization

Adds basic AspNetCore Authorization Middleware capabilities to Azure Functions. (isolated worker process only).

## Purpose of the Pakcage
Users holding a valid Jwt Bearer token that's issued on an AspNetCore app can call Http Triggered Azure Functions with the same token.

## Installation (Nuget)
Install the [`ShiftSoftware.Azure.Functions.AspNetCore.Authorization`](https://www.nuget.org/packages/ShiftSoftware.Azure.Functions.AspNetCore.Authorization) package

## Registration (Program.cs)
Using **`ConfigureFunctionsWorkerDefaults`**:   

``` C#
    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults(x =>
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
<br/>

Using **`ConfigureFunctionsWebApplication`**:   

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

## Using [Authorize] on AzureFunction Class and AzureFunction Mehotds 
After registering the Middleware, the **`[Authorize]`** works in a similar manner as it does in AspNetCore applications.

```C#
    [Function("hello-http-response-data")]
    [Authorize]
    public async Task<HttpResponseData> SayHello([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Hello");

        return response;
    }
```
or
```C#
    [Function("hello-iaction-result")]
    [Authorize]
    public IActionResult SayHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
```
**NOTE:** Using IActionResult as above only works when **`ConfigureFunctionsWebApplication`** is used in `Program.cs`

<br/>

You can also put the **`[Authorize]`** attribute on a calss that contains multiple functions.

```C#
[Authorize]
public class AuthorizeOnClassAndAllowAnonymous
{
    [Function("authorize-on-class-http-response-data")]
    public async Task<HttpResponseData> SayHello([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteStringAsync("Hello");

        return response;
    }

    [Function("authorize-on-class-iaction-result")]
    public IActionResult SayHello2([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Hello");
    }
}
```

**NOTE:** Using IActionResult as shown on the second function only works when **`ConfigureFunctionsWebApplication`** is used in `Program.cs`

## Using [AllowAnonymous]


## Getting Claims



[comment]: <> (Advanced Usage: Writing Custom Attributes on [Authorize])
