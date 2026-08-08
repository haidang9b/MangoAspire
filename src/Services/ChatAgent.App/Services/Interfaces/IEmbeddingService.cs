namespace ChatAgent.App.Services.Interfaces;

/// <summary>
/// Thin wrapper over the Azure OpenAI embedding generator that is always registered, even
/// when embeddings are switched off. Callers branch on <see cref="IsEnabled"/> and treat a
/// null result as "no vector available", which is what lets retrieval fall back to
/// full-text search instead of failing.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>True when a deployment is configured and embeddings are enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Embeds one string. Returns null when disabled or when the call fails.</summary>
    Task<ReadOnlyMemory<float>?> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embeds a batch, preserving order. Returns null when disabled or when the call
    /// fails; individual entries are null only if the provider returned fewer vectors
    /// than inputs.
    /// </summary>
    Task<IReadOnlyList<ReadOnlyMemory<float>?>?> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
