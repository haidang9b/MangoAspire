using Mango.Core.Domain;

namespace Coupons.API.Entities;

public class Coupon : EntityBase<Guid>
{
    public required string Code { get; set; }

    public decimal DiscountAmount { get; set; }
}
