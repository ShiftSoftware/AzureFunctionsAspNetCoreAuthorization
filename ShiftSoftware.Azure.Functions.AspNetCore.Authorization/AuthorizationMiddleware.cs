using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Services;
using ShiftSoftware.Azure.Functions.AspNetCore.Authorization.Utilities;
using System.Net;
using System.Security.Claims;

namespace ShiftSoftware.Azure.Functions.AspNetCore.Authorization;

internal class AuthorizationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TokenService tokenService;
    private readonly IHttpContextAccessor? httpContextAccessor;
    private readonly IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>? authorizationOptions;
    private readonly IAuthorizationService? authorizationService;

    public AuthorizationMiddleware(
        TokenService tokenService,
        IHttpContextAccessor? httpContextAccessor = null,
        IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>? authorizationOptions = null,
        IAuthorizationService? authorizationService = null)
    {
        this.tokenService = tokenService;
        this.httpContextAccessor = httpContextAccessor;
        this.authorizationOptions = authorizationOptions;
        this.authorizationService = authorizationService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext()!;

        if (httpContextAccessor is not null)
            httpContextAccessor.HttpContext = httpContext;

        Dictionary<string, ClaimsPrincipal?> shcemeClaims = null;
        ClaimsPrincipal? claims = null;

        //Set user to prevent null reference exception
        context.Items["User"] = new ClaimsPrincipal();

        var request = httpContext.Request;
        var authorizationHeader = request?.Headers?.Authorization;
        var authorizationHeaderValue = authorizationHeader.GetValueOrDefault().FirstOrDefault();

        // Get the token from the request header
        var token = authorizationHeaderValue?.Replace("Bearer ", "");

        shcemeClaims = tokenService.ValidateToken(token);

        var methodInfo = context.GetTargetFunctionMethod();
        var authorizeAttribute = AttributeUtility.GetAttribute<AuthorizeAttribute>(methodInfo);
        var anonymousAttribute = AttributeUtility.GetAttribute<AllowAnonymousAttribute>(methodInfo);

        if (HasAuthorizeEffect(authorizeAttribute, anonymousAttribute))
        {
            var schemes = ParseSchemes(authorizeAttribute.GetValueOrDefault().attribute?.AuthenticationSchemes);

            if (schemes is not null)
                claims = shcemeClaims.FirstOrDefault(x => schemes.Contains(x.Key) && x.Value is not null).Value;
            else
                claims = shcemeClaims.FirstOrDefault().Value;

            if (claims is null)
            {
                await new UnauthorizedResult().ExecuteResultAsync(new ActionContext
                {
                    HttpContext = httpContext
                });
                return;
            }
            else
            {
                // Set the user to the context
                context.Items["User"] = claims!;
                httpContext.User = claims!;
                httpContext.Items["User"] = claims!;
                
                // Check if the user is in the required role
                var roles = ParseRoles(authorizeAttribute.GetValueOrDefault().attribute?.Roles);
                if (roles is not null)
                {
                    if (!UserIsInRole(claims, roles))
                    {
                        await new ForbidResult().ExecuteResultAsync(new ActionContext
                        {
                            HttpContext = httpContext
                        });
                        return;
                    }
                }

                // Check if the user is in the required policy
                var policies = ParsePolicies(authorizeAttribute.GetValueOrDefault().attribute?.Policy);
                if (policies is not null)
                {
                    if (!UserIsHasPolicy(claims, policies))
                    {
                        await new StatusCodeResult(StatusCodes.Status403Forbidden).ExecuteResultAsync(new ActionContext
                        {
                            HttpContext = httpContext
                        });
                        return;
                    }
                }

                if (authorizeAttribute!.Value.attribute is IAuthorizationFilter authorizationFilter)
                {
                    // Create a new RouteData object
                    var routeData = httpContext.GetRouteData();

                    //Get action descriptor and filters
                    var endpoint = httpContext.GetEndpoint();
                    var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                    var filters = endpoint?.Metadata.GetMetadata<IEnumerable<IFilterMetadata>>();

                    var authorizationContext = new AuthorizationFilterContext(new ActionContext(httpContext, routeData,
                        actionDescriptor ?? new ActionDescriptor()), filters?.ToList() ?? new List<IFilterMetadata> { });
                    
                    authorizationFilter.OnAuthorization(authorizationContext);
                    var result = authorizationContext.Result;
                    
                    if (result is ForbidResult forbidResult)
                    {
                        await new StatusCodeResult(StatusCodes.Status403Forbidden).ExecuteResultAsync(new ActionContext
                        {
                            HttpContext = httpContext
                        });
                        return;
                    }
                    else
                    {
                        var objrctResult = result as ObjectResult;
                        var statusCodeResult = result as StatusCodeResult;

                        await new ObjectResult(objrctResult?.Value) { StatusCode = objrctResult?.StatusCode ?? statusCodeResult?.StatusCode }
                        .ExecuteResultAsync(new ActionContext
                        {
                            HttpContext = httpContext,
                        });
                        return;
                    }
                }

                await next(context);
            }
        }
        else
        {
            claims = shcemeClaims?.FirstOrDefault(x => x.Value != null).Value;
            
            context.Items["User"] = claims!;
            httpContext.User = claims!;
            httpContext.Items["User"] = claims!;

            await next(context);
        }
    }

    private async Task WriteToReponse(HttpResponseData response,object value, HttpStatusCode statusCode)
    {
        if(value is null)
        {
            response.StatusCode = statusCode;
            return;
        }

        if(value.GetType().IsValueType)
        {
            await response.WriteStringAsync(value.ToString());
            response.StatusCode = statusCode;
            return;
        }
        else
        {
            await response.WriteAsJsonAsync(value);
            response.StatusCode = statusCode;
            return;
        }
    }

    private bool HasAuthorizeEffect((AuthorizeAttribute? attribute, AttributeTargets attributeTargets, int prentLevel)? authorize,
        (AllowAnonymousAttribute? attribute, AttributeTargets attributeTargets, int prentLevel)? anonymous)
    {
        if (authorize is null)
            return false;

        if (anonymous is null)
            return true;

        if (authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Class &&
            anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Method)
            return false;
        else if (authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Method &&
            anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Class)
            return true;
        else if (authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Class &&
            anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Class ||
            authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Method &&
            anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Method &&
            authorize.GetValueOrDefault().prentLevel <= anonymous.GetValueOrDefault().prentLevel)
            return true;

        if (authorize.GetValueOrDefault().prentLevel == anonymous.GetValueOrDefault().prentLevel)
        {
            if (authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Method &&
                               anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Class)
                return true;
            else if (authorize.GetValueOrDefault().attributeTargets == AttributeTargets.Class &&
                               anonymous.GetValueOrDefault().attributeTargets == AttributeTargets.Method)
                return false;
            else
                return true;
        }
        else
        {
            return authorize.GetValueOrDefault().prentLevel < anonymous.GetValueOrDefault().prentLevel;
        }
    }

    private IEnumerable<string>? ParseSchemes(string? schemes)
    {
        if (schemes is null)
            return null;

        if (schemes.Contains(","))
            return schemes.Split(",");
        else
            return new List<string>() { schemes };
    }

    private IEnumerable<string>? ParseRoles(string? roles)
    {
        if (roles is null)
            return null;

        if (roles.Contains(","))
            return roles.Split(",");
        else
            return new List<string>() { roles };
    }

    private IEnumerable<string>? ParsePolicies(string? policies)
    {
        if (policies is null)
            return null;

        if (policies.Contains(","))
            return policies.Split(",");
        else
            return new List<string>() { policies };
    }

    private bool UserIsInRole(ClaimsPrincipal user, IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (user.IsInRole(role))
                return true;
        }

        return false;
    }

    private bool UserIsHasPolicy(ClaimsPrincipal user, IEnumerable<string> policies)
    {
        if (authorizationOptions is null || authorizationService is null)
            return false;

        foreach (var policyName in policies)
        {
            var policy = authorizationOptions.Value.GetPolicy(policyName);
            if (policy is not null)
            {
                var result = authorizationService.AuthorizeAsync(user, policy);
                if (result.Result.Succeeded)
                    return true;
            }
        }

        return false;
    }
}
