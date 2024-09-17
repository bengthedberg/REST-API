namespace Movies.API.Auth;

public static class IdentityExtension
{
    public static Guid? GetUserId(this HttpContext httpContext)
    {
        var userId = httpContext.User.Claims.SingleOrDefault(c => c.Type == APIAuthorizationConstants.UserIdClaimName)?.Value;
        if (userId == null)
        {
            return null;
        }

        return Guid.Parse(userId);
    }
}
