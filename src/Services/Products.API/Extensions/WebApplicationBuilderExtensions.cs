using Mango.ServiceDefaults;

namespace Products.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.AddNpgsqlDataSource(connectionName: "productdb");

        builder.Services.AddServices(builder.Configuration);

        builder.EnrichNpgsqlDbContext<ProductDbContext>();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        return builder;
    }
}
