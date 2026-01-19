using EventBus.Events;

namespace ShoppingCart.API.IntegrationEvents;

public record CartCheckedOutEvent : IntegrationEvent
{
    public Guid CartId { get; set; }

    public required string UserId { get; set; }

    public string? CouponCode { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime PickupDate { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? CardNumber { get; set; }

    public string? CVV { get; set; }

    public string? ExpiryMonthYear { get; set; }

    public int CartTotalItems { get; set; }

    public IEnumerable<CartDetailsDto>? CartDetails { get; set; }

    public record CartDetailsDto
    {
        public Guid Id { get; set; }

        public Guid CartHeaderId { get; set; }

        public Guid ProductId { get; set; }

        public int Count { get; set; }
    }
}
