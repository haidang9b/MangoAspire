namespace Mango.Core.Caching;

/// <summary>
/// Lifetime settings for a single cache entry. Any property left null falls
/// back to the default configured on the cache itself.
/// </summary>
public sealed record CacheEntryOptions
{
    /// <summary>
    /// Overall lifetime of the entry, applied to the distributed (L2) layer
    /// when one is configured.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Lifetime of the entry in the in-process (L1) layer. Defaults to
    /// <see cref="Expiration"/> when not set.
    /// </summary>
    public TimeSpan? LocalExpiration { get; init; }
}
