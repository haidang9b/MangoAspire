using Mango.Infrastructure;
using Mango.Infrastructure.ProcessedMessages;
using Microsoft.EntityFrameworkCore;

namespace Products.API.Data;

public class ProductDbContext : AppDbContextBase
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<CatalogBrand> CatalogBrands { get; set; }

    public DbSet<CatalogType> CatalogTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
        modelBuilder.ApplyProcessedMessageConfiguration();
    }
}
