using Mango.ServiceDefaults;
using ShoppingCart.API.Data;

namespace ShoppingCart.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.AddNpgsqlDataSource(connectionName: "shoppingcartdb");

        builder.Services.AddServices(builder.Configuration);

        builder.EnrichNpgsqlDbContext<ShoppingCartDbContext>();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        return builder;
    }
}
