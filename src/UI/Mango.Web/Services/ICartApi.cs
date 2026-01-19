using Mango.Core.Domain;
using Mango.Web.Models;
using Refit;

namespace Mango.Web.Services;

public interface ICartApi
{
    [Get("/api/carts/GetCart/{userId}")]
    Task<ResultModel<CartDto>> GetCartByUserIdAsync(string userId);

    [Post("/api/carts/AddCart")]
    Task<ResultModel<CartDto>> AddToCartAsync([Body] CartDto cartDto);

    [Put("/api/carts/UpdateCart")]
    Task<ResultModel<CartDto>> UpdateCartAsync([Body] CartDto cartDto);

    [Delete("/api/carts/RemoveCart/{cartId}")]
    Task<ResultModel<bool>> RemoveFromCartAsync(int cartId);

    [Post("/api/carts/ApplyCoupon")]
    Task<ResultModel<bool>> ApplyCouponAsync([Body] CartDto cartDto);

    [Post("/api/carts/RemoveCoupon")]
    Task<ResultModel<bool>> RemoveCouponAsync([Body] string userId);

    [Post("/api/carts/Checkout")]
    Task<ResultModel<bool>> CheckoutAsync([Body] CartHeaderDto cartHeaderDto);
}
