using Mango.Core.Domain;

namespace Orders.API.Entities;

public class OrderDetails : EntityBase<Guid>
{
    public Guid OrderHeaderId { get; set; }

    public virtual OrderHeader CartHeader { get; set; } = null!;

    public Guid ProductId { get; set; }

    public int Count { get; set; }

    public string? ProductName { get; set; }

    public decimal Price { get; set; }
}
