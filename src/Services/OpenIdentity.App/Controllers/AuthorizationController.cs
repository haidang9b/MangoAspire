using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIdentity.App.Controllers;

public class AuthorizationController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : Controller
{

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    // Anti-forgery is intentionally disabled: this endpoint receives
    // form-encoded requests from OAuth clients, not from a same-origin form.
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!authenticateResult.Succeeded)
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                },
                IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(authenticateResult.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        var principal = await CreatePrincipalAsync(user);

        principal.SetScopes(request.GetScopes());
        principal.SetResources("mango");

        // Include the ID context
        principal.SetDestinations(GetDestinations);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(Claims.Subject, request.ClientId ?? throw new InvalidOperationException());

            // Fix: Add explicit destination mapping for client credentials flow.
            identity.SetDestinations(_ => [Destinations.AccessToken]);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var subject = authenticateResult.Principal?.GetClaim(Claims.Subject);
            if (string.IsNullOrEmpty(subject))
            {
                return Forbid(
                    new AuthenticationProperties
                    {
                        Items =
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }
                    },
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var user = await userManager.FindByIdAsync(subject);
            if (user == null)
            {
                return Forbid(
                    new AuthenticationProperties
                    {
                        Items =
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }
                    },
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (!await signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    new AuthenticationProperties
                    {
                        Items =
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                        }
                    },
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await CreatePrincipalAsync(user);
            principal.SetScopes(request.GetScopes());
            principal.SetResources("mango");
            principal.SetDestinations(GetDestinations);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Userinfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(
                new AuthenticationProperties
                {
                    Items =
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                    }
                },
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = await userManager.GetUserIdAsync(user),
            [Claims.Name] = user.Name ?? await userManager.GetUserNameAsync(user),
            [Claims.Email] = await userManager.GetEmailAsync(user),
            [Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user)
        };

        var displayName = user.Name ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var (given, family) = SplitName(displayName);
            claims[Claims.GivenName] = given;
            claims[Claims.FamilyName] = family;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 1)
        {
            claims[Claims.Role] = roles[0];
        }
        else if (roles.Count > 1)
        {
            claims[Claims.Role] = roles;
        }

        return Ok(claims);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.Email:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case Claims.Subject:
            case Claims.Role:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    private static (string GivenName, string FamilyName) SplitName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var subject = principal.GetClaim(Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            var userId = await userManager.GetUserIdAsync(user);
            principal.SetClaim(Claims.Subject, userId);
        }

        var name = user.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            principal.SetClaim(Claims.Name, name);

            var (given, family) = SplitName(name);
            if (!string.IsNullOrWhiteSpace(given))
            {
                principal.SetClaim(Claims.GivenName, given);
            }

            if (!string.IsNullOrWhiteSpace(family))
            {
                principal.SetClaim(Claims.FamilyName, family);
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0 && principal.Identity is ClaimsIdentity identity)
        {
            identity.SetClaims(Claims.Role, [..roles]);
        }

        return principal;
    }
}
