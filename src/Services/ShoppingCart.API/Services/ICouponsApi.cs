using Mango.Core.Domain;
using Refit;
using ShoppingCart.API.Dtos;

namespace ShoppingCart.API.Services;

public interface ICouponsApi
{
    [Get("/api/coupons/{couponCode}")]
    Task<ResultModel<CouponDto>> GetCouponAsync(string couponCode);
}
