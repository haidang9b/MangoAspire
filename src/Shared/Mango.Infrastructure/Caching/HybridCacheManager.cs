using Mango.Core.Caching;
using Microsoft.Extensions.Caching.Hybrid;

namespace Mango.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheManager"/> backed by <see cref="HybridCache"/>.
/// </summary>
public sealed class HybridCacheManager(HybridCache cache) : ICacheManager
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(key, factory, ToHybridOptions(options), tags, cancellationToken);

    public ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(key, state, factory, ToHybridOptions(options), tags, cancellationToken);

    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => cache.SetAsync(key, value, ToHybridOptions(options), tags, cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(key, cancellationToken);

    public ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(keys, cancellationToken);

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        => cache.RemoveByTagAsync(tag, cancellationToken);

    public ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
        => cache.RemoveByTagAsync(tags, cancellationToken);

    private static HybridCacheEntryOptions? ToHybridOptions(CacheEntryOptions? options)
    {
        if (options is null || (options.Expiration is null && options.LocalExpiration is null))
        {
            return null;
        }

        return new HybridCacheEntryOptions
        {
            Expiration = options.Expiration,
            LocalCacheExpiration = options.LocalExpiration ?? options.Expiration
        };
    }
}
