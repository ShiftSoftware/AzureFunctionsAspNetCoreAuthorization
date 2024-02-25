using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;
using System.Security.Claims;
using System.Text;

var shouldConfigureFunctionsWorkerDefaults = Directory
    .GetFiles(Directory.GetCurrentDirectory())
    .Contains($"{Directory.GetCurrentDirectory()}\\shouldConfigureFunctionsWorkerDefaults");

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = false,
    ValidateIssuer = true,
    ValidIssuer = "Issuer",
    RequireExpirationTime = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("A secure key that's shared between AspNetCore and Azure Functions")),
    ValidateIssuerSigningKey = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
};

IHostBuilder hostBuilder = new HostBuilder();

if (shouldConfigureFunctionsWorkerDefaults)
{
    hostBuilder = hostBuilder
        .ConfigureFunctionsWorkerDefaults(x =>
        {
            x.AddAuthentication()
            .AddJwtBearer(tokenValidationParameters);
        });
}
else
{
    hostBuilder = hostBuilder
        .ConfigureFunctionsWebApplication(x =>
        {
            x.AddAuthentication()
            .AddJwtBearer(tokenValidationParameters);
        });
}

var host = hostBuilder.
    ConfigureServices(services =>
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