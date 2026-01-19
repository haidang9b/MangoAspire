using Mango.Core.Domain;

namespace ShoppingCart.API.Entities;

public class CartHeader : EntityBase<Guid>
{
    public required string UserId { get; set; }

    public string? CouponCode { get; set; }
}
