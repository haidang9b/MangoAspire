using Mango.ServiceDefaults;
using Orders.API.Data;

namespace Orders.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.AddNpgsqlDataSource(connectionName: "orderdb");

        builder.Services.AddServices(builder.Configuration);

        builder.EnrichNpgsqlDbContext<OrdersDbContext>();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        return builder;
    }
}
