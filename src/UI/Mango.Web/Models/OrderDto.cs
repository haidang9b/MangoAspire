namespace Mango.Web.Models;

public class OrderDto
{
    public Guid Id { get; set; }
    public DateTime OrderTime { get; set; }
    public decimal OrderTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class OrderDetailDto
{
    public Guid Id { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime PickupDate { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool PaymentStatus { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CouponCode { get; set; }
    public string? CancelReason { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Count { get; set; }
}
