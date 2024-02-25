using Microsoft.IdentityModel.Tokens;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization;

internal class AuthorizationOptions
{
    public TokenValidationParameters TokenValidationParameters { get; internal set; }
    public string? AuthenticationScheme { get; internal set; }

    public AuthorizationOptions(TokenValidationParameters tokenValidationParameters, string scheme)
    {
        TokenValidationParameters = tokenValidationParameters;
        AuthenticationScheme = scheme;
    }
}