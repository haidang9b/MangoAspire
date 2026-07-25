using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIdentity.App.Data;

public class DbInitializer(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IConfiguration configuration) : IDbInitializer
{
    // Hard-coded seed identities. Must stay in sync with
    // Identity.API/Initializer/DBInitializer.cs so the OIDC subject (user id),
    // username and password are identical no matter which identity provider
    // the AppHost IdentityType switch activates.
    private const string AdminUserId = "a1111111-1111-1111-1111-111111111111";
    private const string AdminPassword = "Admin123*";
    private const string CustomerUserId = "c2222222-2222-2222-2222-222222222222";
    private const string CustomerPassword = "Customer123*";

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
        const string adminRole = "Admin";
        const string customerRole = "Customer";

        // Create roles if they don't exist
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        if (!await roleManager.RoleExistsAsync(customerRole))
        {
            await roleManager.CreateAsync(new IdentityRole(customerRole));
        }

        // Admin seed user
        if (await userManager.FindByNameAsync("admin1@gmail.com") == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = AdminUserId,
                UserName = "admin1@gmail.com",
                Email = "admin1@gmail.com",
                EmailConfirmed = true,
                Name = "John Admin"
            };

            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(adminUser, adminRole);
            await userManager.AddClaimsAsync(adminUser,
            [
                new Claim(Claims.Name, adminUser.Name),
                new Claim(Claims.Role, adminRole),
                new Claim(Claims.Email, adminUser.Email!)
            ]);
        }

        // Customer seed user
        if (await userManager.FindByNameAsync("customer1@gmail.com") == null)
        {
            var customerUser = new ApplicationUser
            {
                Id = CustomerUserId,
                UserName = "customer1@gmail.com",
                Email = "customer1@gmail.com",
                EmailConfirmed = true,
                Name = "Jane Customer"
            };

            var result = await userManager.CreateAsync(customerUser, CustomerPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create customer user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(customerUser, customerRole);
            await userManager.AddClaimsAsync(customerUser,
            [
                new Claim(Claims.Name, customerUser.Name),
                new Claim(Claims.Role, customerRole),
                new Claim(Claims.Email, customerUser.Email!)
            ]);
        }
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
