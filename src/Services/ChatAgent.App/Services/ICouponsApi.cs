using Mango.Core.Domain;
using Refit;

namespace ChatAgent.App.Services;

public interface ICouponsApi
{
    [Get("/api/coupons/{couponCode}")]
    Task<ResultModel<CouponResponseDto>> GetCouponAsync(string couponCode);
}
