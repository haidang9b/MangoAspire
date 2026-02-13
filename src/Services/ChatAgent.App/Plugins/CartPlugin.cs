using ChatAgent.App.Services;
using Mango.RestApis.Requests;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatAgent.App.Plugins;

public class CartPlugin
{
    private readonly ICouponsApi _couponsApi;

    public CartPlugin(ICouponsApi couponsApi)
    {
        _couponsApi = couponsApi;
    }

    [KernelFunction]
    [Description("Get coupon details by code")]
    public async Task<CouponResponseDto?> ValidateCoupount([Description("Coupon code")] string code)
    {
        var result = await _couponsApi.GetCouponAsync(code);

        return result.Data;
    }
}
