using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;

public static class IFunctionWorkerApplicationBuilderExtension
{

    public static AuthenticationBuilder AddAuthentication(this IFunctionsWorkerApplicationBuilder builder)
    {
        return new AuthenticationBuilder(builder);
    }
}
