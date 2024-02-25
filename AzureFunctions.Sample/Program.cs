using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;
using System.Security.Claims;
using System.Text;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(x =>
    {
        x.AddAuthentication()
        .AddJwtBearer(
            new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = "Issuer",
                RequireExpirationTime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("A secure key that's shared between AspNetCore and Azure Functions")),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }
        );
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