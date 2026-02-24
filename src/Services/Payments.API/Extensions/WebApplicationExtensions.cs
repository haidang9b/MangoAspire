using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;

namespace Payments.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseGlobalExceptionHandler();

        app.UseHttpsRedirection();

        app.MapDefaultEndpoints();

        return app;
    }
}
