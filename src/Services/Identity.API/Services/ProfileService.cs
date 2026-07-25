using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Identity.API.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserProfileCache _userProfileCache;

    public ProfileService(UserManager<ApplicationUser> userManager, UserProfileCache userProfileCache)
    {
        _userManager = userManager;
        _userProfileCache = userProfileCache;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        string sub = context.Subject.GetSubjectId();
        var profile = await _userProfileCache.GetAsync(sub);

        if (profile is null)
        {
            context.IssuedClaims = [];
            return;
        }

        List<Claim> claims = profile.Claims
            .Where(claim => context.RequestedClaimTypes.Contains(claim.Type))
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

        foreach (var roleName in profile.Roles)
        {
            claims.Add(new Claim(JwtClaimTypes.Role, roleName));
            claims.Add(new Claim(JwtClaimTypes.FamilyName, profile.LastName ?? string.Empty));
            claims.Add(new Claim(JwtClaimTypes.GivenName, profile.FirstName ?? string.Empty));
        }

        claims.AddRange(profile.RoleClaims.Select(claim => new Claim(claim.Type, claim.Value)));

        context.IssuedClaims = claims;
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        // Deliberately not cached: this is the gate that stops a deleted user
        // from continuing to exchange tokens, so it must see current state.
        string sub = context.Subject.GetSubjectId();
        var user = await _userManager.FindByIdAsync(sub);
        context.IsActive = user != null;
    }
}
