using Mango.Core.Caching;

namespace OpenIdentity.App.Services;

/// <summary>
/// A user's profile fields and roles in a serializable shape, so it can be
/// held in the cache.
/// </summary>
public sealed record UserProfileSnapshot
{
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}

/// <summary>
/// Caches the per-user lookups behind the OpenID Connect endpoints. The
/// authorize, token and userinfo endpoints each need the user's profile and
/// roles, which otherwise costs a database round-trip per request.
/// </summary>
public sealed class UserProfileCache(
    ICacheManager cacheManager,
    UserManager<ApplicationUser> userManager)
{
    /// <summary>Tag applied to every entry, so all users can be evicted at once.</summary>
    public const string CacheTag = "openidentity:user-profiles";

    // Deliberately short: role changes are only picked up when the entry
    // expires or is explicitly invalidated.
    private static readonly CacheEntryOptions CacheOptions = new() { Expiration = TimeSpan.FromMinutes(5) };

    public ValueTask<UserProfileSnapshot?> GetAsync(string userId, CancellationToken cancellationToken = default)
        => cacheManager.GetOrCreateAsync(
            CacheKey(userId),
            (Cache: this, UserId: userId),
            static (state, ct) => state.Cache.LoadAsync(state.UserId, ct),
            CacheOptions,
            [CacheTag],
            cancellationToken);

    /// <summary>Evicts a single user, for use after their roles or profile change.</summary>
    public ValueTask InvalidateAsync(string userId, CancellationToken cancellationToken = default)
        => cacheManager.RemoveAsync(CacheKey(userId), cancellationToken);

    /// <summary>Evicts every cached user, for use after a role-wide change.</summary>
    public ValueTask InvalidateAllAsync(CancellationToken cancellationToken = default)
        => cacheManager.RemoveByTagAsync(CacheTag, cancellationToken);

    private static string CacheKey(string userId) => $"openidentity:user-profile:{userId}";

    private async ValueTask<UserProfileSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new UserProfileSnapshot
        {
            UserId = userId,
            UserName = user.UserName,
            Name = user.Name,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = [.. roles]
        };
    }
}
