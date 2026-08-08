using ChatAgent.App.Data.Enums;
using NpgsqlTypes;
using Pgvector;

namespace ChatAgent.App.Data.Entities;

/// <summary>
/// One retrievable unit of text — a product blurb, a category name, or a knowledge-base
/// chunk — with its embedding and full-text index. Products, categories and documents all
/// share this table so there is a single HNSW index, a single GIN index, and a single
/// search path in <see cref="Services.KnowledgeSearchService"/>.
/// </summary>
public class VectorDocument
{
    public Guid Id { get; set; }

    public VectorSourceType SourceType { get; set; }

    /// <summary>
    /// Key of the row this was built from, as text because sources are keyed differently
    /// (product = Guid, category = int, knowledge chunk = owning document Guid).
    /// </summary>
    public required string SourceId { get; set; }

    /// <summary>The text that gets embedded and indexed.</summary>
    public required string Content { get; set; }

    /// <summary>Source-specific extras (product name, price, image, heading breadcrumb).</summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Populated asynchronously by <see cref="Services.EmbeddingBackfillService"/>.
    /// Null means "not embedded yet" and is what the backfill query selects on.
    /// </summary>
    public Vector? Embedding { get; set; }

    public DateTime? EmbeddedAt { get; set; }

    /// <summary>
    /// Stored generated column (<c>to_tsvector</c> over <see cref="Content"/>). Never
    /// assigned in code — Postgres maintains it, and it backs the full-text fallback.
    /// </summary>
    public NpgsqlTsVector? SearchVector { get; set; }

    public DateTime UpdatedAt { get; set; }
}
