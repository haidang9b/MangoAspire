using System.ComponentModel.DataAnnotations.Schema;

namespace ShoppingCart.API.Entities;

public class CartDetails : EntityBase<Guid>
{
    public Guid CartHeaderId { get; set; }

    public int Count { get; set; }

    [ForeignKey(nameof(CartHeaderId))]
    public virtual CartHeader CartHeader { get; set; } = null!;

    public Guid ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}
