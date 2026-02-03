using Mango.Core.Domain;

namespace ShoppingCart.API.Entities;

public class CartHeader : EntityBase<Guid>
{
    public required string UserId { get; set; }

    public string? CouponCode { get; set; }

    public virtual ICollection<CartDetails> CartDetails { get; set; } = new List<CartDetails>();
}
