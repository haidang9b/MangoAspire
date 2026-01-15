using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Products.API.Data;
using Products.API.ExceptionHandlers;

namespace Products.API.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        services.AddOpenApi();

        services.AddPostgresDbContext<ProductDbContext>(
            configuration.GetConnectionString("productdb")
                ?? throw new ArgumentNullException("productdb"),
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

        return services;

    }
}
