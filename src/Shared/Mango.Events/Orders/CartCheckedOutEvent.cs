using EventBus.Events;

namespace Mango.Events.Orders;

public record CartCheckedOutEvent : IntegrationEvent
{
    public Guid CartId { get; set; }

    public required string UserId { get; set; }

    public required string CouponCode { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTime PickupDate { get; set; }

    public string? Phone { get; set; }

    public required string Email { get; set; }

    public required string CardNumber { get; set; }

    public required string CVV { get; set; }

    public string? ExpiryMonthYear { get; set; }

    public int CartTotalItems { get; set; }

    public IEnumerable<CartDetailsDto> CartDetails { get; set; } = [];

    public record CartDetailsDto
    {
        public Guid Id { get; set; }

        public Guid CartHeaderId { get; set; }

        public int Count { get; set; }

        public Guid ProductId { get; set; }
    }
}
