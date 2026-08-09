using Azure.AI.OpenAI;
using ChatAgent.App.Data;
using ChatAgent.App.Data.EntityTypeConfigurations;
using ChatAgent.App.Guards;
using ChatAgent.App.Guards.Authorization;
using ChatAgent.App.Guards.Grounding;
using ChatAgent.App.Guards.Interfaces;
using ChatAgent.App.Guards.Output;
using ChatAgent.App.Guards.Untrusted;
using ChatAgent.App.Plugins;
using EventBus.Abstractions;
using Mango.Core.Options;
using Mango.Infrastructure.Behaviors;
using Mango.Infrastructure.Extensions;
using Mango.Infrastructure.Interceptors;
using Mediator.Abstractions;
using Mediator.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Brings pgvector's UseVector() extension on NpgsqlDbContextOptionsBuilder into scope.
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Refit;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace ChatAgent.App.Extensions;

public static class IServiceCollectionExtensions
{
    /// <summary>Named client carrying the resilience pipeline for Azure OpenAI calls.</summary>
    private const string AzureOpenAiClientName = "azure-openai";

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
            },
            // Registers pgvector's type mapping and the vector distance functions used by
            // KnowledgeSearchService.
            doMoreNpgsqlOptionsConfigure: npgsqlOptions => npgsqlOptions.UseVector());

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

        services.AddDocumentApi("ChatAgent", "v1", "ChatAgent");

        services.Configure<AIAgentConfiguration>(configuration.GetSection(AIAgentConfiguration.SectionName));
        services.AddAIAgent(configuration);

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
                // Duende emits one claim per scope; OpenIddict emits a single
                // space-delimited scope claim. Accept both formats.
                policy.RequireAssertion(context => context.User.FindAll("scope")
                    .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Contains("mango"));
            });
        });

        services.AddCurrentUserContext();

        services.AddChatRateLimiting(configuration);

        // Backs the relevance guard's cached category lexicon.
        services.AddCacheManager();

        services.AddSingleton<IChatHistoryMemoryStorage, ChatHistoryMemoryStorage>();
        services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
        services.AddScoped<IAgentService, AgentService>();

        services.AddRetrieval();
        services.AddGuards();

        return services;
    }

    /// <summary>
    /// Local read-model retrieval: the vector index, its search paths, and the knowledge
    /// base ingestion pipeline.
    /// </summary>
    private static IServiceCollection AddRetrieval(this IServiceCollection services)
    {
        services.AddScoped<IVectorIndexer, VectorIndexer>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IKnowledgeSearchService, KnowledgeSearchService>();
        services.AddScoped<IKnowledgeBaseSeeder, KnowledgeBaseSeeder>();
        services.AddSingleton<IMarkdownChunker, MarkdownChunker>();

        services.AddHostedService<EmbeddingBackfillService>();

        return services;
    }

    /// <summary>
    /// The two customer-facing guardrails plus the grounding capture that makes output
    /// verification a fact check rather than a second opinion.
    /// </summary>
    private static IServiceCollection AddGuards(this IServiceCollection services)
    {
        services.AddScoped<GuardChatClient>();
        services.AddScoped<IRelevanceGuard, RelevanceGuard>();
        services.AddScoped<IResponseGuard, ResponseGuard>();

        // Scoped so one request's captured tool results can never leak into another's
        // verification.
        services.AddScoped<IGroundingContext, GroundingContext>();
        services.AddScoped<GroundingCaptureFilter>();

        // Scoped for the same reason, and for one more: the fence's strength comes from a nonce
        // the untrusted content cannot predict, and a nonce shared across requests is one an
        // earlier conversation could have taught an attacker.
        services.AddScoped<IUntrustedFence, UntrustedFence>();

        // Stateless and pure, so a singleton costs nothing.
        services.AddSingleton<IAnswerFactChecker, AnswerFactChecker>();

        // Pre-execution authorization. Scoped because the rules depend on the signed-in customer
        // and the budget is per turn.
        services.AddScoped<IToolAuthorizer, ToolAuthorizer>();
        services.AddScoped<ToolAuthorizationFilter>();
        services.AddScoped<ToolWriteBudget>();

        return services;
    }

    private static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceUrls = configuration.GetSection(ServiceUrlsOptions.SectionName).Get<ServiceUrlsOptions>()
                ?? new ServiceUrlsOptions();

        // Coupons and carts stay as live HTTP calls: they are transactional writes against
        // another service's state, so a local replica would be wrong. Products are not
        // here any more — they arrive over CDC and are read from the local database.
        //
        // AddRefitGeneratedClient, not AddRefitClient: Refit 14 makes the reflection
        // request builder an opt-in package, so the plain overload throws at resolution
        // time unless Refit.Reflection is installed. Both interfaces generate inline (no
        // RF006), so the generated implementations are used directly.
        services.AddRefitGeneratedClient<ICouponsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.CouponsApi))
            .AddAuthToken();

        services.AddRefitGeneratedClient<ICartApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(serviceUrls.ShoppingCartApi))
            .AddAuthToken();

        // Add HttpClient for Bing Search
        services.AddHttpClient("BingSearch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    private static IServiceCollection AddAIAgent(this IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.GetSection(AIAgentConfiguration.SectionName).Get<AIAgentConfiguration>()
            ?? throw new ArgumentNullException(AIAgentConfiguration.SectionName);

        var apiUrl = config.ApiUrl ?? throw new ArgumentNullException(nameof(AIAgentConfiguration.ApiUrl));
        var apiKey = config.ApiKey ?? throw new ArgumentNullException(nameof(AIAgentConfiguration.ApiKey));

        // Routed through IHttpClientFactory so AddServiceDefaults' standard resilience handler
        // actually applies. Constructed bare, the SDK uses its own pipeline and never sees that
        // handler, which is why model calls had no timeout at all: a wedged request held the
        // customer's turn open for as long as their browser stayed connected.
        services.AddHttpClient(AzureOpenAiClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(config.Http.TotalTimeoutSeconds);
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(config.Http.AttemptTimeoutSeconds);
                resilience.Retry.MaxRetryAttempts = config.Http.MaxRetries;

                // The handler refuses to start unless the sampling window is at least twice the
                // attempt timeout, so this has to move with it rather than keeping its default.
                resilience.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(config.Http.AttemptTimeoutSeconds * 2);
            });

        // Deferred to resolution time, which is the earliest point IHttpClientFactory exists.
        services.AddSingleton(serviceProvider =>
        {
            var httpClient = serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(AzureOpenAiClientName);

            return new AzureOpenAIClient(
                new Uri(apiUrl),
                new ApiKeyCredential(apiKey),
                new AzureOpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(httpClient),

                    // Retries belong to the resilience handler; leaving them on here as well
                    // would multiply out to attempts nobody budgeted for.
                    RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
                });
        });

        var modelId = config.ModelId ?? throw new ArgumentNullException(nameof(AIAgentConfiguration.ModelId));

        // A null client means "resolve one from the service provider", which is what lets the
        // registration above supply the factory-built transport.
        var kernelBuilder = services.AddKernel()
            .AddAzureOpenAIChatCompletion(modelId, azureOpenAIClient: null);

        // A separate, usually cheaper, deployment for guard calls. Registered under a
        // service key so GuardChatClient can pick it up without disturbing the main
        // completion service the agent uses.
        if (!string.IsNullOrWhiteSpace(config.Guard.ModelId) && config.Guard.ModelId != modelId)
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                config.Guard.ModelId,
                azureOpenAIClient: null,
                serviceId: GuardChatClient.GuardServiceKey);
        }

        if (config.Embedding.IsConfigured)
        {
            if (config.Embedding.Dimensions != VectorDocumentConfiguration.EmbeddingDimensions)
            {
                // The column type and HNSW index are fixed-width, so a mismatch would fail
                // on every insert. Better to refuse to start than to fill the queue with
                // documents that can never be embedded.
                throw new InvalidOperationException(
                    $"AIAgent:Embedding:Dimensions is {config.Embedding.Dimensions} but the vector column is " +
                    $"{VectorDocumentConfiguration.EmbeddingDimensions}. Changing embedding models requires a migration.");
            }

            // SKEXP0010: the embedding-generator overload is still marked experimental in
            // Semantic Kernel 1.78. It is the supported replacement for the obsolete
            // AddAzureOpenAITextEmbeddingGeneration, so the diagnostic is suppressed here
            // rather than avoided.
#pragma warning disable SKEXP0010
            kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
                config.Embedding.DeploymentName!, azureOpenAIClient: null);
#pragma warning restore SKEXP0010
        }

        services.AddScoped<ICartPlugin, CartPlugin>();
        services.AddScoped<IProductsPlugin, ProductsPlugin>();
        services.AddScoped<ICouponsPlugin, CouponsPlugin>();
        services.AddScoped<ICheckoutPlugin, CheckoutPlugin>();
        services.AddScoped<IWebSearchPlugin, WebSearchPlugin>();

        return services;
    }
}
