using ShoppingCart.API.Dtos;

namespace ShoppingCart.API.Services;

public interface ICouponsApi
{
    Task<CouponDto> GetCouponAsync(string couponCode);
}
