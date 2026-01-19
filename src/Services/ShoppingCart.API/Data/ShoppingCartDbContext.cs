using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Entities;

namespace ShoppingCart.API.Data;

public class ShoppingCartDbContext : AppDbContextBase
{
    public ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<CartHeader> CartHeaders { get; set; }

    public DbSet<CartDetails> CartDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingCartDbContext).Assembly);

    }
}
