using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Movies.API.Auth;

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(APIAuthorizationConstants.ApiKeyHeaderName, out var requestedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key is missing");
            return;
        }
        else 
        {
            var apiKey = _configuration.GetValue<string>("Api:Key")!;
            if (apiKey != requestedApiKey)
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key");
                return;
            }
        }
        
    }
}
