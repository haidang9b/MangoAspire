namespace Mango.RestApis.Requests;

public class AddToCartRequestDto
{
    public required Guid ProductId { get; set; }

    public int Count { get; set; }

    public string? CouponCode { get; set; }
}
