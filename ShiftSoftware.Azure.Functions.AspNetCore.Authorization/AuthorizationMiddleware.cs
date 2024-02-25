using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
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
        Dictionary<string, ClaimsPrincipal?> shcemeClaims = null;
        ClaimsPrincipal? claims = null;

        //Set user to prevent null reference exception
        context.Items["User"] = new ClaimsPrincipal();

        var request = await context.GetHttpRequestDataAsync();
        var authorizationHeader = request?.Headers?.FirstOrDefault(x => x.Key.ToLower() == "Authorization".ToLower());
        var authorizationHeaderValue = authorizationHeader.GetValueOrDefault().Key is not null ?
            authorizationHeader!.Value.Value.FirstOrDefault() : null;

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
                var response = request?.CreateResponse(HttpStatusCode.Unauthorized);
                context.GetInvocationResult().Value = response;
                return;
            }
            else
            {
                // Set the user to the context
                context.Items["User"] = claims!;
                request?.Identities.ToList().AddRange(claims?.Identities);

                // Check if the user is in the required role
                var roles = ParseRoles(authorizeAttribute.GetValueOrDefault().attribute?.Roles);
                if (roles is not null)
                {
                    if (!UserIsInRole(claims, roles))
                    {
                        var response = request?.CreateResponse(HttpStatusCode.Forbidden);
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                }

                // Check if the user is in the required policy
                var policies = ParsePolicies(authorizeAttribute.GetValueOrDefault().attribute?.Policy);
                if (policies is not null)
                {
                    if (!UserIsHasPolicy(claims, policies))
                    {
                        var response = request?.CreateResponse(HttpStatusCode.Forbidden);
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                }
                
                var httpContext = CreateHttpContext(request, context, claims);
                SetHttpContextToIHttpContextAccessor(httpContext);

                if (authorizeAttribute!.Value.attribute is IAuthorizationFilter authorizationFilter)
                {
                    // Get route parameters from the request
                    var routeParameters = context.BindingContext.BindingData;

                    // Create a new RouteData object
                    var routeData = new RouteData();

                    // Add route parameters to the RouteData object
                    foreach (var parameter in routeParameters)
                    {
                        if (parameter.Value is string value)
                        {
                            routeData.Values[parameter.Key] = value;
                        }
                    }

                    var authorizationContext = new AuthorizationFilterContext(new ActionContext(httpContext, routeData, new ActionDescriptor()),
                        new List<IFilterMetadata> { });
                    
                    authorizationFilter.OnAuthorization(authorizationContext);

                    var result = authorizationContext.Result;
                    if(result is StatusCodeResult statusCodeResult)
                    {
                        var response = request?.CreateResponse((HttpStatusCode)statusCodeResult.StatusCode);
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                    else if(result is ForbidResult forbidResult)
                    {
                        var response = request?.CreateResponse(HttpStatusCode.Forbidden);
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                    else if (result is ObjectResult objectResult)
                    {
                        var response = request?.CreateResponse();
                        await WriteToReponse(response, objectResult.Value, (HttpStatusCode)objectResult.StatusCode);
                        context.GetInvocationResult().Value = response;
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
            request?.Identities.ToList().AddRange(claims?.Identities);
            var httpContext = CreateHttpContext(request, context, claims);
            SetHttpContextToIHttpContextAccessor(httpContext);

            await next(context);
        }
    }

    private HttpContext CreateHttpContext(HttpRequestData request, FunctionContext context, ClaimsPrincipal? claims)
    {
        //Setup HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = context.InstanceServices;
        httpContext.User = claims ?? new ClaimsPrincipal();
        httpContext.Request.Method = request.Method;
        httpContext.Request.Headers.ToList()
            .AddRange(request.Headers.Select(x => new KeyValuePair<string, StringValues>(x.Key, new StringValues(x.Value.ToArray()))));
        httpContext.Request.Scheme = request.Url.Scheme;
        httpContext.Request.Host = new HostString(request.Url.Host, request.Url.Port);
        httpContext.Request.QueryString = new QueryString(request.Url.Query);
        httpContext.Request.Path = request.Url.AbsolutePath;
        httpContext.Items = context.Items;
        httpContext.TraceIdentifier = context.TraceContext.TraceParent;

        return httpContext;
    }

    private void SetHttpContextToIHttpContextAccessor(HttpContext httpContext)
    {
        if (httpContextAccessor is not null)
            httpContextAccessor.HttpContext = httpContext;
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
