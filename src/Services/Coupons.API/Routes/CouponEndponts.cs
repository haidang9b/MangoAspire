using Coupons.API.Features.Coupons;

namespace Coupons.API.Routes;

public static class CouponEndponts
{
    extension(WebApplication app)
    {
        public WebApplication MapCouponEndpoints()
        {
            var routeGroupBuilder = app.MapGroup("/api/coupons");

            routeGroupBuilder.MapGet("/{code}", async (string code, ISender sender) =>
            {
                return await sender.Send(new GetCoupon.Query
                {
                    Code = code
                });
            });

            return app;
        }
    }
}
