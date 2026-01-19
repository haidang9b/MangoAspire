using Mango.Core.Domain;
using Mango.Web.Models;
using Refit;

namespace Mango.Web.Services;

public interface ICouponsApi
{
    [Get("/api/coupon/{couponCode}")]
    Task<ResultModel<CouponDto>> GetCouponAsync(string couponCode);
}
