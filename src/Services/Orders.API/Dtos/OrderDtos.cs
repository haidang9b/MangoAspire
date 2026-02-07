namespace Orders.API.Dtos;

public record OrderDto
{
    public Guid Id { get; set; }
    public DateTime OrderTime { get; set; }
    public decimal OrderTotal { get; set; }
    public required string Status { get; set; }
    public int ItemCount { get; set; }
}

public record OrderDetailDto
{
    public Guid Id { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime PickupDate { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public required string Status { get; set; }
    public bool PaymentStatus { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? CouponCode { get; set; }
    public string? CancelReason { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public record OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Count { get; set; }
}
