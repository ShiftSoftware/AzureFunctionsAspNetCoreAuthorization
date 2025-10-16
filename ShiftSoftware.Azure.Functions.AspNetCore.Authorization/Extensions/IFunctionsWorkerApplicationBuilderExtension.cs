using Microsoft.Azure.Functions.Worker;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;

public static class IFunctionsWorkerApplicationBuilderExtension
{
    public static AuthenticationBuilder AddAuthentication(this IFunctionsWorkerApplicationBuilder builder)
    {
        return new AuthenticationBuilder(builder);
    }
}
