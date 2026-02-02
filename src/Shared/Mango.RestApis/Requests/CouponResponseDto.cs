namespace Mango.RestApis.Requests;

public class CouponResponseDto
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public decimal DiscountAmount { get; set; }
}
