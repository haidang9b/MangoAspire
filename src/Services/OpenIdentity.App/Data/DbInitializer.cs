using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIdentity.App.Data;

public class DbInitializer(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
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
                UserName = "admin1@gmail.com",
                Email = "admin1@gmail.com",
                EmailConfirmed = true,
                Name = "John Admin"
            };

            var password = configuration["Identity:AdminPassword"] ?? throw new InvalidOperationException("Admin password not configured.");
            var result = await userManager.CreateAsync(adminUser, password);
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
                UserName = "customer1@gmail.com",
                Email = "customer1@gmail.com",
                EmailConfirmed = true,
                Name = "Jane Customer"
            };

            var password = configuration["Identity:CustomerPassword"] ?? throw new InvalidOperationException("Customer password not configured.");
            var result = await userManager.CreateAsync(customerUser, password);
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
        if (await scopeManager.FindByNameAsync("api") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api",
                DisplayName = "API Access",
                Resources = { "resource_server" }
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
        var webClientId = configuration["OpenIddict:Clients:MangoWeb:ClientId"] ?? "mango-web";
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
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api",
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
                    Permissions.Prefixes.Scope + "api"
                }
            });
        }
    }
}
