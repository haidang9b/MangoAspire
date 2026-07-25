namespace Mango.Core.Caching;

/// <summary>
/// Application-facing cache abstraction. The implementation is backed by
/// HybridCache, which serves reads from an in-process (L1) cache and falls
/// through to a distributed (L2) cache when one is registered, while
/// collapsing concurrent misses for the same key into a single factory call.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, invoking
    /// <paramref name="factory"/> to produce and store it on a miss.
    /// </summary>
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Overload that passes <paramref name="state"/> to the factory, so the
    /// callback can stay static and avoid a closure allocation per call.
    /// </summary>
    ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="value"/>, replacing any existing entry.</summary>
    ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Evicts a single entry.</summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Evicts several entries.</summary>
    ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>Evicts every entry written with the given tag.</summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>Evicts every entry written with any of the given tags.</summary>
    ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
