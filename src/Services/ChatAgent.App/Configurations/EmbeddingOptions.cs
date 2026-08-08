using ChatAgent.App.Data.EntityTypeConfigurations;

namespace ChatAgent.App.Configurations;

/// <summary>
/// Azure OpenAI embedding settings for the retrieval index. Everything degrades
/// gracefully: with <see cref="Enabled"/> false or <see cref="DeploymentName"/> blank, no
/// embeddings are generated and retrieval falls back to Postgres full-text search.
/// </summary>
public class EmbeddingOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Azure OpenAI deployment name, e.g. <c>text-embedding-3-small</c>.</summary>
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Must match <see cref="VectorDocumentConfiguration.EmbeddingDimensions"/>, since the
    /// column type and HNSW index are fixed-width. Validated at startup.
    /// </summary>
    public int Dimensions { get; set; } = VectorDocumentConfiguration.EmbeddingDimensions;

    /// <summary>How many pending documents to embed per backfill pass.</summary>
    public int BatchSize { get; set; } = 32;

    /// <summary>
    /// Cosine distance cut-off for a semantic hit (0 = identical, 2 = opposite). Hits
    /// beyond this are discarded so an unrelated question returns nothing rather than the
    /// least-bad match.
    /// </summary>
    public double MaxCosineDistance { get; set; } = 0.55;

    public int BackfillIntervalSeconds { get; set; } = 30;

    /// <summary>True when embeddings are switched on and a deployment has been supplied.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(DeploymentName);
}
