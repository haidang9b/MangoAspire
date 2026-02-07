using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mango.SagaOrchestrators.Data;
using Mango.SagaOrchestrators.ExceptionHandlers;
using Microsoft.EntityFrameworkCore;

namespace Mango.SagaOrchestrators.Extensions;

public static class IServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddServices(IConfiguration configuration)
        {
            // Add services to the container.
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            services.AddOpenApi();

            services.AddPostgresDbContext<SagaDbContext>(
                configuration.GetConnectionString("sagaorchestratorsdb")
                    ?? throw new ArgumentNullException("sagaorchestratorsdb"),
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


            services.AddCurrentUserContext();

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

}
