using Mango.ServiceDefaults;

namespace Payments.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.Services.AddServices(builder.Configuration);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        return builder;
    }
}
