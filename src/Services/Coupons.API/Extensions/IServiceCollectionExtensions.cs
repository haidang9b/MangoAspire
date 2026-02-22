using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.ExceptionHandlers;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mediator.Extensions;

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

        services.AddMediator(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TxBehavior<,>));


        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddEndpointsApiExplorer();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddDocumentApi("Coupons API", "v1", "Coupons API");
        return services;

    }
}
