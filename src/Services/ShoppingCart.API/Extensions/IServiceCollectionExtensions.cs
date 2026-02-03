using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Refit;
using ShoppingCart.API.Data;
using ShoppingCart.API.ExceptionHandlers;
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
            services.AddCurrentUserContext();


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = "https://localhost:8080";
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

            services.AddRefitClient<ICouponsApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://coupons-api"))
                .AddAuthToken();
            return services;
        }
    }

}
