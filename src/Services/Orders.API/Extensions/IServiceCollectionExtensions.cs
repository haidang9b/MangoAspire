using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mango.Infrastructure.ProcessedMessages;
using Mediator.Extensions;
using Microsoft.EntityFrameworkCore;
using Orders.API.Behaviours;
using Orders.API.Services;

namespace Orders.API.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        services.AddOpenApi();

        services.AddPostgresDbContext<OrdersDbContext>(
            configuration.GetConnectionString("orderdb")
                ?? throw new ArgumentNullException("orderdb"),
            doMoreDbContextOptionsConfigure: (sp, options) =>
            {
                options.AddInterceptors(
                    sp.GetRequiredService<PerformanceInterceptor>());
            });

        services.AddScoped<PerformanceInterceptor>();

        services.AddMediator(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentifiedBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.AddProcessedMessages<OrdersDbContext>();

        services.AddScoped<IIntegrationEventService, IntegrationEventService>();

        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddEndpointsApiExplorer();

        services.AddGlobalExceptionHandler();

        services.AddDocumentApi("Orders API", "v1", "Orders API");

        services.AddCurrentUserContext();

        services.AddMangoJwtAuthentication(configuration);

        return services;
    }
}
