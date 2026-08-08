using ChatAgent.App.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;

namespace ChatAgent.App.Services;

/// <summary>
/// Drains the embedding queue — <c>vector_documents</c> rows whose <c>embedded_at</c> is
/// null — in the background.
/// </summary>
/// <remarks>
/// Embedding is deliberately decoupled from the writers. CDC handlers and the knowledge
/// seeder only ever mark a row as needing a vector, so neither a slow Azure OpenAI call nor
/// an outage can dead-letter a CDC message or block startup. Until a row is embedded it
/// remains searchable through the full-text path.
/// </remarks>
public class EmbeddingBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<EmbeddingBackfillService> _logger;

    public EmbeddingBackfillService(
        IServiceScopeFactory scopeFactory,
        IOptions<AIAgentConfiguration> options,
        ILogger<EmbeddingBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value.Embedding;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogInformation(
                "Embeddings are not configured; retrieval will use full-text search only.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.BackfillIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Keep going while work remains, so a large initial ingest is not paced at
                // one batch per interval.
                while (await ProcessBatchAsync(stoppingToken) > 0)
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Embedding backfill pass failed; retrying in {Interval}.", interval);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <returns>How many documents were embedded, or 0 when the queue is empty or stalled.</returns>
    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatAgentDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var pending = await dbContext.VectorDocuments
            .Where(x => x.EmbeddedAt == null)
            .OrderBy(x => x.UpdatedAt)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var vectors = await embeddingService.EmbedBatchAsync(
            [.. pending.Select(x => x.Content)],
            cancellationToken);

        if (vectors is null)
        {
            // Provider failure — leave the rows queued and let the next pass retry them.
            return 0;
        }

        var embedded = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            var vector = i < vectors.Count ? vectors[i] : null;
            if (vector is null)
            {
                continue;
            }

            if (vector.Value.Length != _options.Dimensions)
            {
                // A dimension mismatch means the deployment does not match the column
                // width; storing it would fail on the HNSW index. Flag loudly instead.
                _logger.LogError(
                    "Embedding for document {DocumentId} has {Actual} dimensions but the column expects {Expected}. Check AIAgent:Embedding:DeploymentName.",
                    pending[i].Id, vector.Value.Length, _options.Dimensions);
                continue;
            }

            pending[i].Embedding = new Vector(vector.Value);
            pending[i].EmbeddedAt = DateTime.UtcNow;
            embedded++;
        }

        if (embedded == 0)
        {
            return 0;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Embedded {Count} knowledge documents.", embedded);

        return embedded;
    }
}
