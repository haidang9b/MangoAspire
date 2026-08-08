using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Products.API.Data.EntityTypeConfigurations;

/// <summary>
/// Maps <see cref="DebeziumSignal"/> onto the exact table shape the connector expects.
/// </summary>
/// <remarks>
/// Column names and lengths are Debezium's contract, not ours, so they are pinned explicitly
/// rather than left to the snake_case naming convention — a mismatch here means the connector
/// silently never sees a signal.
/// </remarks>
public class DebeziumSignalConfiguration : IEntityTypeConfiguration<DebeziumSignal>
{
    public void Configure(EntityTypeBuilder<DebeziumSignal> builder)
    {
        builder.ToTable("debezium_signal");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(42)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Data)
            .HasColumnName("data")
            .HasMaxLength(2048);
    }
}
