using ChatAgent.App.Data;
using ChatAgent.App.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ChatAgent.App.Services;

/// <inheritdoc cref="IKnowledgeSearchService"/>
/// <remarks>
/// Three tiers, tried in order, so retrieval keeps working as capability drops away:
/// <list type="number">
/// <item>semantic — pgvector cosine distance over the HNSW index;</item>
/// <item>full text — <c>websearch_to_tsquery</c> over the GIN index, used whenever
/// embeddings are off, the embedding call failed, or the semantic pass found nothing;</item>
/// <item>fuzzy — <c>ILIKE</c> on the longest query word, so a single distinctive term
/// still matches when the tsquery parser rejects the phrase.</item>
/// </list>
/// </remarks>
public class KnowledgeSearchService : IKnowledgeSearchService
{
    private const string TextSearchConfig = "english";

    private readonly ChatAgentDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<KnowledgeSearchService> _logger;

    public KnowledgeSearchService(
        ChatAgentDbContext dbContext,
        IEmbeddingService embeddingService,
        IOptions<AIAgentConfiguration> options,
        ILogger<KnowledgeSearchService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _options = options.Value.Embedding;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        VectorSourceType[] sourceTypes,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || sourceTypes.Length == 0 || topK <= 0)
        {
            return [];
        }

        var semanticHits = await TrySemanticSearchAsync(query, sourceTypes, topK, cancellationToken);
        if (semanticHits.Count > 0)
        {
            return semanticHits;
        }

        var fullTextHits = await FullTextSearchAsync(query, sourceTypes, topK, cancellationToken);
        if (fullTextHits.Count > 0)
        {
            return fullTextHits;
        }

        return await FuzzySearchAsync(query, sourceTypes, topK, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHit>> TrySemanticSearchAsync(
        string query,
        VectorSourceType[] sourceTypes,
        int topK,
        CancellationToken cancellationToken)
    {
        // EmbedAsync returns null when embeddings are disabled or the provider failed;
        // either way the caller drops through to full-text search.
        var queryVector = await _embeddingService.EmbedAsync(query, cancellationToken);
        if (queryVector is null)
        {
            return [];
        }

        var vector = new Vector(queryVector.Value);
        var maxDistance = _options.MaxCosineDistance;

        var rows = await _dbContext.VectorDocuments
            .AsNoTracking()
            .Where(x => sourceTypes.Contains(x.SourceType) && x.Embedding != null)
            .Select(x => new
            {
                x.SourceType,
                x.SourceId,
                x.Content,
                x.Metadata,
                Distance = x.Embedding!.CosineDistance(vector),
            })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Semantic search for {Query} returned {Count} hits.", query, rows.Count);

        return [.. rows.Select(x => new SearchHit(
            x.SourceType, x.SourceId, x.Content, x.Metadata, x.Distance, SearchMatchMode.Semantic))];
    }

    private async Task<IReadOnlyList<SearchHit>> FullTextSearchAsync(
        string query,
        VectorSourceType[] sourceTypes,
        int topK,
        CancellationToken cancellationToken)
    {
        // websearch_to_tsquery tolerates raw user input (quotes, OR, -negation) instead of
        // throwing on syntax the way to_tsquery does.
        var rows = await _dbContext.VectorDocuments
            .AsNoTracking()
            .Where(x => sourceTypes.Contains(x.SourceType)
                && x.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery(TextSearchConfig, query)))
            .Select(x => new
            {
                x.SourceType,
                x.SourceId,
                x.Content,
                x.Metadata,
                Rank = x.SearchVector!.RankCoverDensity(EF.Functions.WebSearchToTsQuery(TextSearchConfig, query)),
            })
            .OrderByDescending(x => x.Rank)
            .Take(topK)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Full-text search for {Query} returned {Count} hits.", query, rows.Count);

        return [.. rows.Select(x => new SearchHit(
            x.SourceType, x.SourceId, x.Content, x.Metadata, x.Rank, SearchMatchMode.FullText))];
    }

    private async Task<IReadOnlyList<SearchHit>> FuzzySearchAsync(
        string query,
        VectorSourceType[] sourceTypes,
        int topK,
        CancellationToken cancellationToken)
    {
        // The longest word is the most distinctive one, and skipping short words avoids
        // matching every row on "the" or "a".
        var term = query
            .Split([' ', ',', '.', '?', '!', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3)
            .OrderByDescending(w => w.Length)
            .FirstOrDefault();

        if (term is null)
        {
            return [];
        }

        var pattern = $"%{term}%";

        var rows = await _dbContext.VectorDocuments
            .AsNoTracking()
            .Where(x => sourceTypes.Contains(x.SourceType) && EF.Functions.ILike(x.Content, pattern))
            .OrderBy(x => x.Content.Length)
            .Take(topK)
            .Select(x => new { x.SourceType, x.SourceId, x.Content, x.Metadata })
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Fuzzy search for {Term} returned {Count} hits.", term, rows.Count);

        return [.. rows.Select(x => new SearchHit(
            x.SourceType, x.SourceId, x.Content, x.Metadata, 0d, SearchMatchMode.Fuzzy))];
    }
}
