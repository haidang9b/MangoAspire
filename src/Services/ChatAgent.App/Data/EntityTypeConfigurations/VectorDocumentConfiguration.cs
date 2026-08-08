using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatAgent.App.Data.EntityTypeConfigurations;

public class VectorDocumentConfiguration : IEntityTypeConfiguration<VectorDocument>
{
    /// <summary>
    /// Output dimension of text-embedding-3-small. It has to be baked into the column
    /// type for pgvector to accept an HNSW index, so switching embedding models means a
    /// migration — <c>AIAgentEmbeddingOptions.Dimensions</c> is validated against this.
    /// </summary>
    public const int EmbeddingDimensions = 1536;

    public void Configure(EntityTypeBuilder<VectorDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .IsRequired();

        builder.Property(x => x.SourceId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb");

        builder.Property(x => x.Embedding)
            .HasColumnType($"vector({EmbeddingDimensions})");

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Maintained by Postgres, never assigned in code. Raw SQL, so it refers to the
        // physical column name produced by the snake_case naming convention.
        builder.Property(x => x.SearchVector)
            .HasComputedColumnSql("to_tsvector('english', coalesce(content, ''))", stored: true);

        // Approximate nearest-neighbour index for the semantic path. Cosine, to match the
        // distance operator used in KnowledgeSearchService.
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // Backs the full-text fallback used when embeddings are unavailable.
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN");

        // Retrieval always filters by source type; re-indexing looks a row up by origin.
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => new { x.SourceType, x.SourceId });

        // Drives the backfill queue scan (WHERE embedded_at IS NULL).
        builder.HasIndex(x => x.EmbeddedAt);
    }
}
