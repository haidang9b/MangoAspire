using ChatAgent.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatAgent.App.Data.EntityTypeConfigurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        // Id is the upstream catalog_types key, replicated verbatim — never generated here.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // See ProductConfiguration — tombstoned rather than removed, so the replay fence
        // keeps its watermark.
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
