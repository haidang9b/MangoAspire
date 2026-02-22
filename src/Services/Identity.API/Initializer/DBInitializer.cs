using Duende.IdentityModel;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Identity.API.Initializer;

public class DBInitializer : IDBInitializer
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public DBInitializer(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task InitializesAsync()
    {
        var adminRole = _configuration["IdentityServer:AdminRole"] ?? "Admin";
        var customerRole = _configuration["IdentityServer:CustomerRole"] ?? "Customer";

        if (_roleManager.FindByNameAsync(adminRole).Result == null)
        {
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
            await _roleManager.CreateAsync(new IdentityRole(customerRole));
        }
        else
        {
            return;
        }

        ApplicationUser adminUser = new ApplicationUser()
        {
            UserName = "admin1@gmail.com",
            Email = "admin1@gmail.com",
            EmailConfirmed = true,
            PhoneNumber = "111111111111",
            FirstName = "Jhon",
            LastName = "Admin"
        };

        await _userManager.CreateAsync(adminUser, "Admin123*");
        await _userManager.AddToRoleAsync(adminUser, adminRole);

        var tempAdminUser = await _userManager.AddClaimsAsync(adminUser, new Claim[] {
            new Claim(JwtClaimTypes.Name, $"{adminUser.FirstName} {adminUser.LastName}"),
            new Claim(JwtClaimTypes.GivenName, adminUser.FirstName),
            new Claim(JwtClaimTypes.FamilyName, adminUser.LastName),
            new Claim(JwtClaimTypes.Role, adminRole),
        });

        ApplicationUser customerUser = new ApplicationUser()
        {
            UserName = "customer1@gmail.com",
            Email = "customer1@gmail.com",
            EmailConfirmed = true,
            PhoneNumber = "111111111111",
            FirstName = "Jane",
            LastName = "Customer"
        };

        await _userManager.CreateAsync(customerUser, "Customer123*");
        await _userManager.AddToRoleAsync(customerUser, customerRole);

        var tempCustomerUser = await _userManager.AddClaimsAsync(customerUser, new Claim[] {
            new Claim(JwtClaimTypes.Name, $"{customerUser.FirstName} {customerUser.LastName}"),
            new Claim(JwtClaimTypes.GivenName, customerUser.FirstName),
            new Claim(JwtClaimTypes.FamilyName, customerUser.LastName),
            new Claim(JwtClaimTypes.Role, customerRole),
        });
    }
}
