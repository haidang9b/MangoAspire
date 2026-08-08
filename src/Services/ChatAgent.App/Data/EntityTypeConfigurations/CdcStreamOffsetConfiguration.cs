using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatAgent.App.Data.EntityTypeConfigurations;

public class CdcStreamOffsetConfiguration : IEntityTypeConfiguration<CdcStreamOffset>
{
    public void Configure(EntityTypeBuilder<CdcStreamOffset> builder)
    {
        builder.HasKey(x => x.StreamName);

        builder.Property(x => x.StreamName)
            .HasMaxLength(255);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
