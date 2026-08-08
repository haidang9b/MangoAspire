using ChatAgent.App.Data.Enums;

namespace ChatAgent.App.Services.Interfaces;

/// <summary>
/// Maintains the <c>vector_documents</c> retrieval index. Implementations stage changes on
/// the ambient <see cref="Data.ChatAgentDbContext"/> without saving, so a caller can commit
/// the source-of-truth row and its index entry in one <c>SaveChangesAsync</c>.
/// </summary>
/// <remarks>
/// Upserts deliberately leave <c>EmbeddedAt</c> null rather than calling the embedding
/// model inline. Embedding happens later in <see cref="EmbeddingBackfillService"/>, so a
/// slow or failing Azure OpenAI call can never dead-letter a CDC message.
/// </remarks>
public interface IVectorIndexer
{
    /// <summary>
    /// Inserts or updates the index entry for a source row. The embedding is invalidated
    /// only when <paramref name="content"/> actually changed, so a no-op CDC replay does
    /// not trigger re-embedding.
    /// </summary>
    Task UpsertAsync(
        VectorSourceType sourceType,
        string sourceId,
        string content,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>Drops the index entry for a source row that no longer exists.</summary>
    Task RemoveAsync(
        VectorSourceType sourceType,
        string sourceId,
        CancellationToken cancellationToken = default);
}
