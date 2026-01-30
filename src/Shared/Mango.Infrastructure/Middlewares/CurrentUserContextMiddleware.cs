using Mango.Core.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mango.Infrastructure.Middlewares;

public class CurrentUserContextMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentUserContext = context.RequestServices.GetRequiredService<ICurrentUserContext>();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            currentUserContext.UserId = context.User.Identity.Name;
            currentUserContext.IsAuthenticated = true;
        }
        else
        {
            currentUserContext.UserId = null;
            currentUserContext.IsAuthenticated = false;
        }

        return next(context);
    }
}
