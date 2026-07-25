using Duende.IdentityModel;
using Identity.API.Models;
using Mango.Core.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Identity.API.Initializer;

public class DBInitializer : IDBInitializer
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SeedUsersOptions _seedUsers;

    public DBInitializer(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<SeedUsersOptions> seedUsers)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _seedUsers = seedUsers.Value;
    }

    public async Task InitializesAsync()
    {
        var admin = _seedUsers.Admin;
        var customer = _seedUsers.Customer;

        admin.Validate($"{SeedUsersOptions.SectionName}:{nameof(SeedUsersOptions.Admin)}");
        customer.Validate($"{SeedUsersOptions.SectionName}:{nameof(SeedUsersOptions.Customer)}");

        if (await _roleManager.FindByNameAsync(admin.Role) == null)
        {
            await _roleManager.CreateAsync(new IdentityRole(admin.Role));
            await _roleManager.CreateAsync(new IdentityRole(customer.Role));
        }
        else
        {
            return;
        }

        await CreateSeedUserAsync(admin);
        await CreateSeedUserAsync(customer);
    }

    private async Task CreateSeedUserAsync(SeedUserOptions seedUser)
    {
        var user = new ApplicationUser
        {
            Id = seedUser.Id,
            UserName = seedUser.UserName,
            Email = seedUser.Email,
            EmailConfirmed = true,
            PhoneNumber = seedUser.PhoneNumber,
            FirstName = seedUser.FirstName,
            LastName = seedUser.LastName
        };

        await _userManager.CreateAsync(user, seedUser.Password);
        await _userManager.AddToRoleAsync(user, seedUser.Role);

        await _userManager.AddClaimsAsync(user, new Claim[]
        {
            new Claim(JwtClaimTypes.Name, seedUser.FullName),
            new Claim(JwtClaimTypes.GivenName, seedUser.FirstName),
            new Claim(JwtClaimTypes.FamilyName, seedUser.LastName),
            new Claim(JwtClaimTypes.Role, seedUser.Role),
        });
    }
}
