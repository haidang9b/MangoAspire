namespace Mango.Core.Options;

/// <summary>
/// Development seed accounts created on first run by the identity providers.
///
/// Both Identity.API (Duende) and OpenIdentity.App (OpenIddict) bind this same
/// shape, and the values must match between them: the AppHost IdentityType
/// switch changes which provider issues tokens, and a differing Id would change
/// the OIDC subject claim that carts and orders are keyed by.
/// </summary>
public class SeedUsersOptions
{
    public const string SectionName = "SeedUsers";

    public SeedUserOptions Admin { get; set; } = new();

    public SeedUserOptions Customer { get; set; } = new();
}

public class SeedUserOptions
{
    /// <summary>Fixed primary key, issued as the OIDC subject claim.</summary>
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Display name, for providers that store a single name field.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Fails fast when a seed account is missing required configuration, rather
    /// than silently creating an account with an empty password.
    /// </summary>
    public void Validate(string sectionPath)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException($"Configuration '{sectionPath}:Id' is required.");
        }

        if (string.IsNullOrWhiteSpace(UserName))
        {
            throw new InvalidOperationException($"Configuration '{sectionPath}:UserName' is required.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException($"Configuration '{sectionPath}:Password' is required.");
        }

        if (string.IsNullOrWhiteSpace(Role))
        {
            throw new InvalidOperationException($"Configuration '{sectionPath}:Role' is required.");
        }
    }
}
