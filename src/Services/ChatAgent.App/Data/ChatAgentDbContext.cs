using ChatAgent.App.Data.Entities;
using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Data;

public class ChatAgentDbContext : AppDbContextBase
{
    public DbSet<ChatMessage> ChatMessages { get; set; }

    /// <summary>Read-model replicated from Products.API by Debezium CDC.</summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>Read-model replicated from Products.API <c>catalog_types</c> by Debezium CDC.</summary>
    public DbSet<ProductCategory> ProductCategories { get; set; }

    /// <summary>Ingestion ledger for knowledge-base markdown files.</summary>
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }

    /// <summary>Unified retrieval index over products, categories and knowledge chunks.</summary>
    public DbSet<VectorDocument> VectorDocuments { get; set; }

    /// <summary>
    /// How far this service has read into each replayable CDC stream. Delete a row to replay
    /// that stream from the beginning.
    /// </summary>
    public DbSet<CdcStreamOffset> CdcStreamOffsets { get; set; }

    public ChatAgentDbContext(DbContextOptions<ChatAgentDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// For derived contexts. Tests use this to swap in a provider that cannot map the
    /// pgvector column type.
    /// </summary>
    protected ChatAgentDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Emits CREATE EXTENSION IF NOT EXISTS vector as part of the migration, so a fresh
        // database bootstraps itself. Requires the pgvector-enabled Postgres image.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatAgentDbContext).Assembly);
    }
}
