namespace Mango.Core.Auth;

/// <summary>
/// Authorization policy names shared by every API. Registered by
/// <c>AddMangoJwtAuthentication()</c> in <c>Mango.Infrastructure</c>.
/// </summary>
public static class AuthPolicies
{
    /// <summary>An authenticated caller whose access token carries the <c>mango</c> scope.</summary>
    public const string ApiScope = "ApiScope";

    /// <summary><see cref="ApiScope"/> plus the <see cref="AuthRoles.Admin"/> role.</summary>
    public const string AdminOnly = "AdminOnly";
}
