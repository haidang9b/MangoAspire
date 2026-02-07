using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Coupons.API.Data;

public class CouponDbContext : AppDbContextBase
{
    public CouponDbContext(DbContextOptions<CouponDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CouponDbContext).Assembly);

    }
}
