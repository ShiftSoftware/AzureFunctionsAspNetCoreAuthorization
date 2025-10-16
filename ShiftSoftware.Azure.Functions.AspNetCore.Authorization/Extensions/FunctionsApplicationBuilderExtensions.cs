using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Extensions;

public static class FunctionsApplicationBuilderExtensions
{
    public static AuthenticationBuilder AddAuthentication(this FunctionsApplicationBuilder builder)
    {
        return new AuthenticationBuilder(builder);
    }
}
