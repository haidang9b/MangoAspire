using Coupons.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coupons.API.Data.EntityTypeConfigurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasData(new Coupon
        {
            Id = Guid.Parse("fcc1596e-7dd5-486f-a498-8eacad314302"),
            Code = "10OFF",
            DiscountAmount = 10,
        });

        builder.HasData(new Coupon
        {
            Id = Guid.Parse("9bb19343-3ba8-492c-8786-469ec240d266"),
            Code = "20OFF",
            DiscountAmount = 20,
        });

        builder.HasData(new Coupon
        {
            Id = Guid.Parse("50aa664d-2984-4159-a0ae-4e3eeaf416c1"),
            Code = "30OFF",
            DiscountAmount = 30,
        });

        builder.HasData(new Coupon
        {
            Id = Guid.Parse("b5a6b6d6-8c2f-4b2e-8f3e-2f5d3e4f5d3e"),
            Code = "40OFF",
            DiscountAmount = 40,
        });
    }
}
