using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Movies.API.Auth;

public class AdminAuthRequirement : IAuthorizationHandler, IAuthorizationRequirement
{
    private readonly string _apiKey;
    private readonly string _apiKeyUserId;

    public AdminAuthRequirement(string apiKey, string apiKeyUserId)
    {
        _apiKey = apiKey;
        _apiKeyUserId = apiKeyUserId;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        // First check if the user is authenticated and has the admin claim
        if (context.User.HasClaim(APIAuthorizationConstants.AdminUserClaimName, "true")) 
        {
            context.Succeed(this);
            return Task.CompletedTask;
        }

        // If the user is not authenticated, check if the request has the api key
        var httpContext = context.Resource as HttpContext;
        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        if (!httpContext.Request.Headers.TryGetValue(APIAuthorizationConstants.ApiKeyHeaderName, out var requestedApiKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }
        else 
        {
            if (_apiKey != requestedApiKey)
            {
                context.Fail();
                return Task.CompletedTask;
            }
        }
        // Add the user id claim to the identity of the api key request
        var identity = (ClaimsIdentity)context.User.Identity!;
        identity.AddClaim(new Claim(APIAuthorizationConstants.UserIdClaimName, _apiKeyUserId));
        context.Succeed(this);
        return Task.CompletedTask;
    }
}
