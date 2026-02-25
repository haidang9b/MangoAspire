using Mango.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace Mango.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public WebApplication MapAllEndpoints()
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

        public WebApplication UseCurrentUserContext()
        {
            app.UseMiddleware<CurrentUserContextMiddleware>();
            return app;
        }

        public WebApplication UseGlobalExceptionHandler()
        {
            app.UseExceptionHandler();
            return app;
        }
    }
}
