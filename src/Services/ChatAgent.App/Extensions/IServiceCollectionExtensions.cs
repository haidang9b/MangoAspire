using Azure.AI.OpenAI;
using ChatAgent.App.Configurations;
using ChatAgent.App.Data;
using ChatAgent.App.Plugins;
using ChatAgent.App.Services;
using Mango.Core.Behaviors;
using Mango.Core.Options;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.ExceptionHandlers;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Refit;
using System.ClientModel;

namespace ChatAgent.App.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        services.AddOpenApi();

        services.AddPostgresDbContext<ChatAgentDbContext>(
            configuration.GetConnectionString("chatagentdb")
                ?? throw new ArgumentNullException("chatagentdb"),
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

        services.AddScoped<IAgentService, AgentService>();


        services.AddDocumentApi("ChatAgent", "v1", "ChatAgent");

        services.AddAIAgent(configuration);
        services.Configure<AIAgentConfiguration>(configuration.GetSection(AIAgentConfiguration.SectionName));

        services.AddApiServices(configuration);

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

        services.AddCurrentUserContext();
        services.AddSingleton<ChatHistoryMemoryStorage>();
        return services;

    }

    private static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {

        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>()
                ?? new ServiceUrlsOptions();
        services.AddRefitClient<ICouponsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.CouponsApi))
            .AddAuthToken();

        services.AddRefitClient<IProductsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ProductsApi))
            .AddAuthToken();

        services.AddRefitClient<ICartApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ShoppingCartApi))
            .AddAuthToken();

        return services;
    }

    private static IServiceCollection AddAIAgent(this IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.GetSection(AIAgentConfiguration.SectionName).Get<AIAgentConfiguration>();
        var client = new AzureOpenAIClient(
            new Uri(config.ApiUrl),
            new ApiKeyCredential(config.ApiKey));

        services.AddKernel()
            .AddAzureOpenAIChatCompletion(config.ModelId, client);

        services.AddScoped<CartPlugin>();
        services.AddScoped<ProductsPlugin>();
        services.AddScoped<CouponsPlugin>();
        return services;
    }
}
