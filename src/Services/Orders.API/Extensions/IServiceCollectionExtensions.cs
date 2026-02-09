using Mango.Core.Behaviors;
using Mango.Core.Options;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mango.Infrastructure.ProcessedMessages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orders.API.ExceptionHandlers;

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

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddOpenBehavior(typeof(IdentifiedBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TxBehavior<,>));
        });
        services.AddProcessedMessages<OrdersDbContext>();

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
