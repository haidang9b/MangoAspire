using Mango.Core.Options;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.ExceptionHandlers;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mango.Infrastructure.ProcessedMessages;
using Mediator.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddDocumentApi("Orders API", "v1", "Orders API");

        services.AddCurrentUserContext();

        // Configure ServiceUrls options
        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>()
            ?? new ServiceUrlsOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = serviceUrls.IdentityApp;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiScope", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "mango");
            });
        });

        return services;
    }
}
