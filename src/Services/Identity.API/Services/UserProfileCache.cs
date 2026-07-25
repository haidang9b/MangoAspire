using Identity.API.Models;
using Mango.Core.Caching;
using Microsoft.AspNetCore.Identity;

namespace Identity.API.Services;

/// <summary>
/// A user's claims and roles, flattened into a serializable shape so it can be
/// held in the cache (<see cref="System.Security.Claims.Claim"/> itself does
/// not round-trip through JSON).
/// </summary>
public sealed record UserProfileSnapshot
{
    public required string UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public required IReadOnlyList<ClaimSnapshot> Claims { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required IReadOnlyList<ClaimSnapshot> RoleClaims { get; init; }
}

public sealed record ClaimSnapshot(string Type, string Value);

/// <summary>
/// Caches the per-user claim and role lookups behind
/// <see cref="ProfileService"/>. IdentityServer calls the profile service on
/// every token issuance and userinfo request, and each call otherwise costs
/// several database round-trips (user, claims, roles, role claims).
/// </summary>
public sealed class UserProfileCache(
    ICacheManager cacheManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
{
    /// <summary>Tag applied to every entry, so all users can be evicted at once.</summary>
    public const string CacheTag = "identity:user-profiles";

    // Deliberately short: role and claim changes are only picked up when the
    // entry expires or is explicitly invalidated.
    private static readonly CacheEntryOptions CacheOptions = new() { Expiration = TimeSpan.FromMinutes(5) };

    public ValueTask<UserProfileSnapshot?> GetAsync(string userId, CancellationToken cancellationToken = default)
        => cacheManager.GetOrCreateAsync(
            CacheKey(userId),
            (Cache: this, UserId: userId),
            static (state, ct) => state.Cache.LoadAsync(state.UserId, ct),
            CacheOptions,
            [CacheTag],
            cancellationToken);

    /// <summary>Evicts a single user, for use after their roles or claims change.</summary>
    public ValueTask InvalidateAsync(string userId, CancellationToken cancellationToken = default)
        => cacheManager.RemoveAsync(CacheKey(userId), cancellationToken);

    /// <summary>Evicts every cached user, for use after a role-wide change.</summary>
    public ValueTask InvalidateAllAsync(CancellationToken cancellationToken = default)
        => cacheManager.RemoveByTagAsync(CacheTag, cancellationToken);

    private static string CacheKey(string userId) => $"identity:user-profile:{userId}";

    private async ValueTask<UserProfileSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        var claims = principal.Claims.Select(c => new ClaimSnapshot(c.Type, c.Value)).ToList();

        var roles = userManager.SupportsUserRole
            ? await userManager.GetRolesAsync(user)
            : [];

        var roleClaims = new List<ClaimSnapshot>();
        if (userManager.SupportsUserRole && roleManager.SupportsRoleClaims)
        {
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role is not null)
                {
                    roleClaims.AddRange((await roleManager.GetClaimsAsync(role))
                        .Select(c => new ClaimSnapshot(c.Type, c.Value)));
                }
            }
        }

        return new UserProfileSnapshot
        {
            UserId = userId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Claims = claims,
            Roles = [.. roles],
            RoleClaims = roleClaims
        };
    }
}
