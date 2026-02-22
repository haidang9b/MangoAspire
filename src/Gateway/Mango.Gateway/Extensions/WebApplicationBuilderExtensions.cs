using Mango.ServiceDefaults;

namespace Mango.Gateway.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddGatewayDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("GatewayPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();

        return builder;
    }
}
