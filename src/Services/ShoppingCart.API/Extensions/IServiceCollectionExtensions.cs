using EventBus.Abstractions;
using Mango.Core.Options;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mediator.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Refit;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Extensions;

public static class IServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddServices(IConfiguration configuration)
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

            // Where the CDC stream reader keeps its position. Delete the row to replay the log.
            services.AddScoped<ICdcOffsetStore, CdcOffsetStore>();

            services.AddMediator(typeof(Program).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TxBehavior<,>));


            services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            services.AddEndpointsApiExplorer();

            services.AddGlobalExceptionHandler();

            services.AddDocumentApi("ShoppingCart API", "v1", "ShoppingCart API");

            // A second AddRefitClient<ICouponsApi> further down configures the base address and
            // token forwarding, and overwrote this one. Harmless, but it registered a client that
            // could never reach anything.
            services.AddCurrentUserContext();

            // Configure ServiceUrls options
            services.Configure<ServiceUrlsOptions>(
                configuration.GetSection(ServiceUrlsOptions.SectionName));

            // Get service URLs from configuration
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
                    // Duende emits one claim per scope; OpenIddict emits a single
                    // space-delimited scope claim. Accept both formats.
                    policy.RequireAssertion(context => context.User.FindAll("scope")
                        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        .Contains("mango"));
                });
            });

            services.AddRefitClient<ICouponsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.CouponsApi))
                .AddAuthToken();
            return services;
        }
    }
}
