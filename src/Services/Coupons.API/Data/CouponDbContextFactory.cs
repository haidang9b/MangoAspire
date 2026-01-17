using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Coupons.API.Data;

public class CouponDbContextFactory : IDesignTimeDbContextFactory<CouponDbContext>
{
    public CouponDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CouponDbContext>();

        optionsBuilder.UseNpgsql("coupondb")
            .UseSnakeCaseNamingConvention();

        return new CouponDbContext(optionsBuilder.Options);
    }
}
