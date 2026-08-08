using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ChatAgent.App.Services;

/// <inheritdoc cref="IEmbeddingService"/>
public class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _generator;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        IOptions<AIAgentConfiguration> options,
        ILogger<EmbeddingService> logger,
        IEmbeddingGenerator<string, Embedding<float>>? generator = null)
    {
        _options = options.Value.Embedding;
        _logger = logger;
        _generator = generator;
    }

    public bool IsEnabled => _options.IsConfigured && _generator is not null;

    public async Task<ReadOnlyMemory<float>?> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            var embedding = await _generator!.GenerateAsync(text, cancellationToken: cancellationToken);
            return embedding.Vector;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Swallowed on purpose: the caller degrades to full-text search rather than
            // failing the customer's question because the embedding endpoint hiccuped.
            _logger.LogWarning(ex, "Embedding generation failed; falling back to keyword search.");
            return null;
        }
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>?>?> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || texts.Count == 0)
        {
            return null;
        }

        try
        {
            var generated = await _generator!.GenerateAsync(texts, cancellationToken: cancellationToken);

            var results = new ReadOnlyMemory<float>?[texts.Count];
            for (var i = 0; i < texts.Count && i < generated.Count; i++)
            {
                results[i] = generated[i].Vector;
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The backfill service retries on its next pass, so a failed batch is not fatal.
            _logger.LogWarning(ex, "Batch embedding generation failed for {Count} documents.", texts.Count);
            return null;
        }
    }
}
