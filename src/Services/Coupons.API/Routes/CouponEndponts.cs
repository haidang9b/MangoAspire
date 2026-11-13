using Coupons.API.Features.Coupons;
using Mango.Core.Auth;

namespace Coupons.API.Routes;

public static class CouponEndponts
{
    extension(WebApplication app)
    {
        public WebApplication MapCouponEndpoints()
        {
            // Every caller (ShoppingCart apply-coupon and checkout, the ChatAgent coupon plugin,
            // Mango.Web's cart, the SPA cart) runs inside a signed-in request and forwards the
            // user's token. Anonymous lookups would let anyone enumerate coupon codes.
            var routeGroupBuilder = app.MapGroup("/api/coupons")
                .RequireAuthorization(AuthPolicies.ApiScope);

            routeGroupBuilder.MapGet("/{code}", async (string code, ISender sender) =>
            {
                return await sender.SendAsync(new GetCoupon.Query
                {
                    Code = code
                });
            });

            return app;
        }
    }
}
