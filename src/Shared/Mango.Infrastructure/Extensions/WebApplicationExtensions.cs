using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace Mango.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapAllEndpoints(this WebApplication app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(IEndpoints).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract);

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoints)Activator.CreateInstance(type)!;
            endpoint.MapEndpoints(app);
        }

        return app;
    }
}
