namespace Mango.Core.Auth;

/// <summary>
/// Role names as issued in the <c>role</c> claim by both identity providers. Role checks compare
/// ordinally, so the casing here must match the <c>SeedUsers</c> configuration exactly.
/// </summary>
public static class AuthRoles
{
    public const string Admin = "Admin";

    /// <summary>The only role self-registration can grant.</summary>
    public const string Customer = "Customer";
}
