namespace Mango.Core.Options;

/// <summary>
/// Configuration options for OpenID Connect authentication.
/// </summary>
public class OpenIdConnectOptions
{
    public const string SectionName = "OpenIdConnect";

    public string Authority { get; set; } = "https://identity-app";
    public string ClientId { get; set; } = "mango";
    public string ClientSecret { get; set; } = "secret";
    public string ResponseType { get; set; } = "code";
    public string[] Scopes { get; set; } = ["mango"];
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
    public bool SaveTokens { get; set; } = true;
    public int CookieExpireMinutes { get; set; } = 10;
}
