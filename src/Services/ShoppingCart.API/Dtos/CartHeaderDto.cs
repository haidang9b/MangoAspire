namespace ShoppingCart.API.Dtos;

public class CartHeaderDto
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public string? CouponCode { get; set; }
}
