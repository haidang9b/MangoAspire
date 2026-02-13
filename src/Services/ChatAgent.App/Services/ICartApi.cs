using Mango.Core.Domain;
using Refit;
using System.ComponentModel.DataAnnotations;

namespace ChatAgent.App.Services;

public interface ICartApi
{
    [Get("/api/carts/{userId}")]
    Task<ResultModel<CartDto>> GetCartByUserIdAsync(string userId);

    [Post("/api/carts")]
    Task<ResultModel<bool>> AddToCartAsync([Body] AddToCartRequestDto cartDto);

    [Delete("/api/carts/item/{cartId}")]
    Task<ResultModel<bool>> RemoveFromCartAsync(Guid cartId);

    [Post("/api/carts/coupon")]
    Task<ResultModel<bool>> ApplyCouponAsync([Body] ApplyCouponRequestDto cartDto);

    [Delete("/api/carts/coupon")]
    Task<ResultModel<bool>> RemoveCouponAsync();
}


public class CartDto
{
    public required CartHeaderDto CartHeader { get; set; }

    public IEnumerable<CartDetailsDto> CartDetails { get; set; } = [];
}

public class CartDetailsDto
{
    public Guid Id { get; set; }

    public virtual ProductDto? Product { get; set; }

    public int Count { get; set; }
}


public class CartHeaderDto
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public string? CouponCode { get; set; }
}



public class ProductDto
{
    public ProductDto()
    {
        Count = 1;
    }
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Catalog Type")]
    public int? CatalogTypeId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public int Stock { get; set; }

    [Range(1, 100)]
    public int Count { get; set; }
}
