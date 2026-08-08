using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Data;

public class ShoppingCartDbContext : AppDbContextBase
{
    public ShoppingCartDbContext(DbContextOptions<ShoppingCartDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<CartHeader> CartHeaders { get; set; }

    public DbSet<CartDetails> CartDetails { get; set; }

    /// <summary>
    /// How far this service has read into each replayable CDC stream. Delete a row to replay
    /// that stream from the beginning.
    /// </summary>
    public DbSet<CdcStreamOffset> CdcStreamOffsets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingCartDbContext).Assembly);

        modelBuilder.Entity<CdcStreamOffset>(entity =>
        {
            entity.HasKey(x => x.StreamName);
            entity.Property(x => x.StreamName).HasMaxLength(255);
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}
