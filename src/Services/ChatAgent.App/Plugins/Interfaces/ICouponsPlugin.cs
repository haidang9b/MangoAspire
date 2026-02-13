namespace ChatAgent.App.Plugins.Interfaces;

public interface ICouponsPlugin
{
    Task<CouponResponseDto?> GetCouponAsync([Description("Coupon code")] string code);
}
