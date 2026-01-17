using Coupons.API.Data;
using Coupons.API.ExceptionHandlers;
using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;

namespace Coupons.API.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        services.AddOpenApi();

        services.AddPostgresDbContext<CouponDbContext>(
            configuration.GetConnectionString("coupondb")
                ?? throw new ArgumentNullException("coupondb"),
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

        services.AddDocumentApi("Coupons API", "v1", "Coupons API");
        return services;

    }
}
