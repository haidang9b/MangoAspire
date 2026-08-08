using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatAgent.App.Data.EntityTypeConfigurations;

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourcePath)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        // Hex SHA-256.
        builder.Property(x => x.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.IngestedAt)
            .IsRequired();

        // One ledger row per file; the seeder upserts on this.
        builder.HasIndex(x => x.SourcePath).IsUnique();
    }
}
