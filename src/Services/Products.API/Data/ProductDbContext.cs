using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Products.API.Entities;

namespace Products.API.Data;

public class ProductDbContext : AppDbContextBase
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);

    }
}
