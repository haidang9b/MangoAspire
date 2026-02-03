using ShoppingCart.API.Dtos;

namespace ShoppingCart.API.Features.Carts.GetCarts;

public class GetCardResponse
{
    public required CartHeaderResponseDto CartHeader { get; set; }

    public IEnumerable<CartDetailsResponseDto> CartDetails { get; set; } = [];

    public class CartHeaderResponseDto
    {
        public Guid Id { get; set; }

        public required string UserId { get; set; }

        public string? CouponCode { get; set; }

        public decimal OrderTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateTime PickupDateTime { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? CardNumber { get; set; }

        public string? CVV { get; set; }

        public string? ExpiryMonthYear { get; set; }
    }

    public class CartDetailsResponseDto
    {
        public Guid Id { get; set; }

        public Guid CartHeaderId { get; set; }

        public virtual CartHeaderDto CartHeader { get; set; } = null!;

        public Guid ProductId { get; set; }

        public virtual ProductResponseDto Product { get; set; } = null!;

        public int Count { get; set; }
    }

    public class ProductResponseDto
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public decimal Price { get; set; }

        public required string Description { get; set; }

        public required string CategoryName { get; set; }

        public required string ImageUrl { get; set; }
    }
}
