using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.ExceptionHandlers;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mango.Orchestrators.Data;
using Mediator.Abstractions;
using Mediator.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Mango.Orchestrators.Extensions;

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

            services.AddMediator(typeof(Program).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TxBehavior<,>));


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
