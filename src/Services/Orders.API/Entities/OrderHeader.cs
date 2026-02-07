namespace Orders.API.Entities;

public class OrderHeader : EntityBase<Guid>
{
    public required string UserId { get; set; }

    public required string CouponCode { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTime PickupDate { get; set; }

    public DateTime OrderTime { get; set; }

    public string? Phone { get; set; }

    public required string Email { get; set; }

    public required string CardNumber { get; set; }

    public string? CVV { get; set; }

    public string? ExpiryMonthYear { get; set; }

    public int CartTotalItems { get; set; }

    public virtual ICollection<OrderDetails> OrderDetails { get; set; } = [];

    public bool PaymentStatus { get; set; }

    public OrderStatus Status { get; set; }

    public string? CancelReason { get; set; }
}
