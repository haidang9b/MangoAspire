namespace Coupons.API.Dtos;

public record CouponDto
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public decimal DiscountAmount { get; set; }
}
