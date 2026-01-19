using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Refit;
using ShoppingCart.API.Data;
using ShoppingCart.API.ExceptionHandlers;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        services.AddOpenApi();

        services.AddPostgresDbContext<ShoppingCartDbContext>(
            configuration.GetConnectionString("shoppingcartdb")
                ?? throw new ArgumentNullException("shoppingcartdb"),
            doMoreDbContextOptionsConfigure: (sp, options) =>
            {
                options.AddInterceptors(
                    sp.GetRequiredService<PerformanceInterceptor>());
            });

        services.AddScoped<PerformanceInterceptor>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TxBehavior<,>));

        });

        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddEndpointsApiExplorer();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddDocumentApi("ShoppingCart API", "v1", "ShoppingCart API");

        services.AddRefitClient<ICouponsApi>();

        return services;
    }
}
