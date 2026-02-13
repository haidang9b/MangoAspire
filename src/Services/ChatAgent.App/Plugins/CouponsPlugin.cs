namespace ChatAgent.App.Plugins;

public class CouponsPlugin : ICouponsPlugin
{
    private readonly ICouponsApi _couponsApi;

    public CouponsPlugin(ICouponsApi couponsApi)
    {
        _couponsApi = couponsApi;
    }

    [KernelFunction]
    [Description("Get coupon details by code")]
    public async Task<CouponResponseDto?> GetCouponAsync([Description("Coupon code")] string code)
    {
        var result = await _couponsApi.GetCouponAsync(code);

        return result.Data;
    }
}
