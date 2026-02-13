//using Mango.Core.Domain;
//using Mango.RestApis.Requests;
//using Mango.Web.Models;
//using Refit;

//namespace ChatAgent.App.Services;

//public interface ICartApi
//{
//    [Get("/api/carts/{userId}")]
//    Task<ResultModel<CartDto>> GetCartByUserIdAsync(string userId);

//    [Post("/api/carts")]
//    Task<ResultModel<bool>> AddToCartAsync([Body] AddToCartRequestDto cartDto);

//    [Put("/api/carts")]
//    Task<ResultModel<CartDto>> UpdateCartAsync([Body] CartDto cartDto);

//    [Delete("/api/carts/item/{cartId}")]
//    Task<ResultModel<bool>> RemoveFromCartAsync(Guid cartId);

//    [Post("/api/carts/coupon")]
//    Task<ResultModel<bool>> ApplyCouponAsync([Body] ApplyCouponRequestDto cartDto);

//    [Delete("/api/carts/coupon")]
//    Task<ResultModel<bool>> RemoveCouponAsync();

//    [Post("/api/carts/checkout")]
//    Task<ResultModel<bool>> CheckoutAsync([Body] CheckoutRequestDto checkoutRequestDto);
//}
