namespace Movies.API.Auth;

public static class APIAuthorizationConstants
{
    public const string AdminUserPolicyName = "Admin";
    public const string AdminUserClaimName = "admin";

    public const string TrustedUserPolicyName = "TrustedUser";
    public const string TrustedUserClaimName = "trustedUser";

    public const string UserIdClaimName = "userid";
}