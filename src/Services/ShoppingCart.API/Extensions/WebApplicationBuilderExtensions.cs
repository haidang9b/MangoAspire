using Mango.ServiceDefaults;
using ShoppingCart.API.Data;

namespace ShoppingCart.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApiDefaults()
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
}
