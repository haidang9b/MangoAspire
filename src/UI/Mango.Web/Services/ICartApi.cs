using Mango.Core.Domain;
using Mango.Web.Models;
using Refit;

namespace Mango.Web.Services;

public interface ICartApi
{
    [Get("/api/carts/{userId}")]
    Task<ResultModel<CartDto>> GetCartByUserIdAsync(string userId);

    [Post("/api/carts")]
    Task<ResultModel<CartDto>> AddToCartAsync([Body] CartDto cartDto);

    [Put("/api/carts")]
    Task<ResultModel<CartDto>> UpdateCartAsync([Body] CartDto cartDto);

    [Delete("/api/carts/item/{cartId}")]
    Task<ResultModel<bool>> RemoveFromCartAsync(Guid cartId);

    [Post("/api/carts/coupon")]
    Task<ResultModel<bool>> ApplyCouponAsync([Body] CartDto cartDto);

    [Post("/api/carts/coupon")]
    Task<ResultModel<bool>> RemoveCouponAsync([Body] string userId);

    [Post("/api/carts/checkout")]
    Task<ResultModel<bool>> CheckoutAsync([Body] CartHeaderDto cartHeaderDto);
}
