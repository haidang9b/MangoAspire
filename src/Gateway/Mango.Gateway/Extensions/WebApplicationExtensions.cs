using Mango.ServiceDefaults;

namespace Mango.Gateway.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseGatewayPipeline(this WebApplication app)
    {
        app.MapDefaultEndpoints();
        app.UseCors("GatewayPolicy");
        app.MapReverseProxy();

        return app;
    }
}
