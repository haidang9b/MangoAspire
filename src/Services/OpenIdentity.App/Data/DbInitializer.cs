using Mango.Core.Options;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIdentity.App.Data;

public class DbInitializer(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IOptions<SeedUsersOptions> seedUsers,
    IConfiguration configuration) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        await SeedRolesAndUsersAsync();
        await SeedScopesAsync();
        await SeedClientsAsync();
    }

    // -------------------------------------------------------
    //  Roles & Users
    // -------------------------------------------------------
    private async Task SeedRolesAndUsersAsync()
    {
        var admin = seedUsers.Value.Admin;
        var customer = seedUsers.Value.Customer;

        admin.Validate($"{SeedUsersOptions.SectionName}:{nameof(SeedUsersOptions.Admin)}");
        customer.Validate($"{SeedUsersOptions.SectionName}:{nameof(SeedUsersOptions.Customer)}");

        // Create roles if they don't exist
        if (!await roleManager.RoleExistsAsync(admin.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(admin.Role));
        }

        if (!await roleManager.RoleExistsAsync(customer.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(customer.Role));
        }

        await CreateSeedUserAsync(admin);
        await CreateSeedUserAsync(customer);
    }

    private async Task CreateSeedUserAsync(SeedUserOptions seedUser)
    {
        if (await userManager.FindByNameAsync(seedUser.UserName) != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = seedUser.Id,
            UserName = seedUser.UserName,
            Email = seedUser.Email,
            EmailConfirmed = true,
            PhoneNumber = seedUser.PhoneNumber,
            Name = seedUser.FullName
        };

        var result = await userManager.CreateAsync(user, seedUser.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create seed user '{seedUser.UserName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, seedUser.Role);
        await userManager.AddClaimsAsync(user,
        [
            new Claim(Claims.Name, user.Name!),
            new Claim(Claims.Role, seedUser.Role),
            new Claim(Claims.Email, user.Email!)
        ]);
    }

    // -------------------------------------------------------
    //  OpenIddict Scopes (Resources)
    // -------------------------------------------------------
    private async Task SeedScopesAsync()
    {
        if (await scopeManager.FindByNameAsync("openid") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "openid",
                DisplayName = "OpenID"
            });
        }

        if (await scopeManager.FindByNameAsync("profile") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "profile",
                DisplayName = "User Profile"
            });
        }

        if (await scopeManager.FindByNameAsync("email") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "email",
                DisplayName = "User Email"
            });
        }

        if (await scopeManager.FindByNameAsync("offline_access") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "offline_access",
                DisplayName = "Offline Access"
            });
        }

        if (await scopeManager.FindByNameAsync("mango") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "mango",
                DisplayName = "API Access",
                Resources = { "mango" }
            });
        }

        if (await scopeManager.FindByNameAsync("roles") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "roles",
                DisplayName = "User Roles"
            });
        }
    }

    // -------------------------------------------------------
    //  OpenIddict Clients (Applications)
    // -------------------------------------------------------
    private async Task SeedClientsAsync()
    {
        // Mango Web — Authorization Code + PKCE
        var webClientId = configuration["OpenIddict:Clients:MangoWeb:ClientId"] ?? "mango";
        if (await applicationManager.FindByClientIdAsync(webClientId) == null)
        {
            var redirectUri = configuration["OpenIddict:Clients:MangoWeb:RedirectUri"] ?? "https://localhost:7002/signin-oidc";
            var postLogoutUri = configuration["OpenIddict:Clients:MangoWeb:PostLogoutUri"] ?? "https://localhost:7002/signout-callback-oidc";
            var secret = configuration["OpenIddict:Clients:MangoWeb:ClientSecret"] ?? throw new InvalidOperationException("Mango Web client secret not configured.");

            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = webClientId,
                ClientSecret = secret,
                DisplayName = "Mango Web",
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Prefixes.Scope + "openid",
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "mango",
                    Permissions.Prefixes.Scope + "roles"
                },
                RedirectUris = { new Uri(redirectUri) },
                PostLogoutRedirectUris = { new Uri(postLogoutUri) },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            });
        }

        // Mango Services — Client Credentials (machine-to-machine)
        var serviceClientId = configuration["OpenIddict:Clients:MangoServices:ClientId"] ?? "mango-services";
        if (await applicationManager.FindByClientIdAsync(serviceClientId) == null)
        {
            var secret = configuration["OpenIddict:Clients:MangoServices:ClientSecret"] ?? throw new InvalidOperationException("Mango Services client secret not configured.");

            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = serviceClientId,
                ClientSecret = secret,
                DisplayName = "Mango Services (M2M)",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "mango"
                }
            });
        }

        // Mango SPA — Authorization Code + PKCE (public client)
        var spaClientId = configuration["OpenIddict:Clients:MangoSpa:ClientId"] ?? "mango-spa";
        if (await applicationManager.FindByClientIdAsync(spaClientId) == null)
        {
            var redirectUri = configuration["OpenIddict:Clients:MangoSpa:RedirectUri"] ?? "http://localhost:5173/callback";
            var silentRedirectUri = configuration["OpenIddict:Clients:MangoSpa:SilentRedirectUri"] ?? "http://localhost:5173/silent-callback";
            var postLogoutUri = configuration["OpenIddict:Clients:MangoSpa:PostLogoutUri"] ?? "http://localhost:5173";

            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = spaClientId,
                DisplayName = "Mango SPA",
                ConsentType = ConsentTypes.Implicit,
                ClientType = ClientTypes.Public,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Prefixes.Scope + "openid",
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "mango",
                    Permissions.Prefixes.Scope + "offline_access"
                },
                RedirectUris =
                {
                    new Uri(redirectUri),
                    new Uri(silentRedirectUri)
                },
                PostLogoutRedirectUris = { new Uri(postLogoutUri) },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            });
        }
    }
}
