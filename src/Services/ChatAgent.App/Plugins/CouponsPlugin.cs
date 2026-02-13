namespace ChatAgent.App.Plugins;

public class CouponsPlugin : ICouponsPlugin
{
    private readonly ICouponsApi _couponsApi;

    public CouponsPlugin(ICouponsApi couponsApi)
    {
        _couponsApi = couponsApi;
    }

    [KernelFunction]
    [Description("Validate and retrieve coupon details including discount amount and type. Use this when the user asks about a coupon code, wants to check if a code is valid, or needs to know the discount value before applying it.")]
    public async Task<CouponResponseDto?> GetCouponAsync([Description("The coupon code to validate and retrieve details for")] string code)
    {
        var result = await _couponsApi.GetCouponAsync(code);

        return result.Data;
    }
}
