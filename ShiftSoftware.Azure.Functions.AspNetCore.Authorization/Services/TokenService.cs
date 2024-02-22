using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Services;

internal class TokenService
{
    private readonly IEnumerable<AuthorizationOptions> listAuthenticationOptions;

    public TokenService(IEnumerable<AuthorizationOptions> listAuthenticationOptions)
    {
        this.listAuthenticationOptions = listAuthenticationOptions;
    }

    internal Dictionary<string, ClaimsPrincipal?> ValidateToken(string? token)
    {
        Dictionary<string, ClaimsPrincipal?> claims = new();

        foreach (var authenticationOptions in listAuthenticationOptions)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, authenticationOptions.TokenValidationParameters
                    , out SecurityToken securityToken);
                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !IsAlgorithmValid(jwtSecurityToken.Header.Alg, authenticationOptions.TokenValidationParameters.ValidAlgorithms))
                    claims.Add(authenticationOptions.AuthenticationScheme ?? "", null);
                else
                    claims.Add(authenticationOptions.AuthenticationScheme ?? "", principal);
            }
            catch (Exception)
            {
                claims.Add(authenticationOptions.AuthenticationScheme ?? "", null);
            }
        }

        return claims;
    }

    private bool IsAlgorithmValid(string algorithm, IEnumerable<string>? validAlgorityhms)
    {
        if (validAlgorityhms == null)
            return true;

        return validAlgorityhms.Any(x => x.Equals(algorithm, StringComparison.InvariantCultureIgnoreCase));
    }
}
