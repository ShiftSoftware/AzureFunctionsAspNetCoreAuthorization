using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization;

public class AuthenticationBuilder
{
    private readonly IFunctionsWorkerApplicationBuilder builder;

    public AuthenticationBuilder(IFunctionsWorkerApplicationBuilder builder)
    {
        this.builder = builder;
    }

    public AuthenticationBuilder AddJwtBearer(string authenticationScheme,
        TokenValidationParameters tokenValidationParameters)
    {
        builder.UseMiddleware<AuthorizationMiddleware>();
        builder.Services.AddScoped(x => new AuthorizationOptions(tokenValidationParameters, authenticationScheme));
        builder.Services.AddScoped<TokenService>();

        return this;
    }

    public AuthenticationBuilder AddJwtBearer(TokenValidationParameters tokenValidationParameters)
    {
        return this.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, tokenValidationParameters);
    }

    public AuthenticationBuilder AddJwtBearer(Action<TokenValidationParameters> tokenValidationParametersBuilder)
    {
        var tokenValidationParameters = new TokenValidationParameters();
        tokenValidationParametersBuilder(tokenValidationParameters);

        return AddJwtBearer(tokenValidationParameters);
    }

    public AuthenticationBuilder AddJwtBearer(string authenticationScheme,
        Action<TokenValidationParameters> tokenValidationParametersBuilder)
    {
        var tokenValidationParameters = new TokenValidationParameters();
        tokenValidationParametersBuilder(tokenValidationParameters);

        return AddJwtBearer(authenticationScheme, tokenValidationParameters);
    }
}
