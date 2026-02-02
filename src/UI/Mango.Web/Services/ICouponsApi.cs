using Mango.Core.Domain;
using Mango.RestApis.Requests;
using Refit;

namespace Mango.Web.Services;

public interface ICouponsApi
{
    [Get("/api/coupons/{couponCode}")]
    Task<ResultModel<CouponResponseDto>> GetCouponAsync(string couponCode);
}
