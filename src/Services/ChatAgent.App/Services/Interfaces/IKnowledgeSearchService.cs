using ChatAgent.App.Data.Enums;

namespace ChatAgent.App.Services.Interfaces;

/// <summary>How a hit was found. Surfaced for logging and for tuning the distance cut-off.</summary>
public enum SearchMatchMode
{
    Semantic = 1,
    FullText = 2,
    Fuzzy = 3,
}

/// <param name="Score">
/// Cosine distance for semantic hits (lower is better) or the text rank for full-text hits
/// (higher is better). Only comparable within one <see cref="SearchMatchMode"/>.
/// </param>
public record SearchHit(
    VectorSourceType SourceType,
    string SourceId,
    string Content,
    string? Metadata,
    double Score,
    SearchMatchMode MatchMode);

/// <summary>
/// The single retrieval entry point over <c>vector_documents</c>. Semantic search when
/// embeddings are configured, Postgres full-text search when they are not.
/// </summary>
public interface IKnowledgeSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        VectorSourceType[] sourceTypes,
        int topK,
        CancellationToken cancellationToken = default);
}
