using Mango.ServiceDefaults;

namespace Coupons.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.AddNpgsqlDataSource(connectionName: "coupondb");
        builder.Services.AddServices(builder.Configuration);

        builder.EnrichNpgsqlDbContext<CouponDbContext>();

        return builder;
    }
}
