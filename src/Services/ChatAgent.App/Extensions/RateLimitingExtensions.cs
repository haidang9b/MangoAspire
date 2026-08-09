using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace ChatAgent.App.Extensions;

/// <summary>
/// Throttling for the chat endpoint.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>Sustained request rate per customer.</summary>
    public const string ChatPolicyName = "chat";

    /// <summary>
    /// In-flight turns per customer, applied alongside <see cref="ChatPolicyName"/>.
    /// </summary>
    /// <remarks>
    /// Two policies rather than one because the two limits answer different questions - how often,
    /// and how many at once - and ASP.NET Core composes them by applying both to the endpoint.
    /// </remarks>
    public const string ChatConcurrencyPolicyName = "chat-concurrency";

    public static IServiceCollection AddChatRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(AIAgentConfiguration.SectionName)
            .GetSection(nameof(AIAgentConfiguration.RateLimit))
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(ChatPolicyName, httpContext =>
            {
                // Registered even when disabled: RequireRateLimiting on the endpoint fails at
                // startup against a policy that does not exist, so switching this off in
                // configuration must not turn into a service that will not boot.
                if (!options.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("disabled");
                }

                return RateLimitPartition.GetSlidingWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        SegmentsPerWindow = options.SegmentsPerWindow,
                        // Never queue: a queued turn holds the connection open while it waits,
                        // which is the resource the limit exists to protect.
                        QueueLimit = 0,
                    });
            });

            limiter.AddPolicy(ChatConcurrencyPolicyName, httpContext =>
            {
                if (!options.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("disabled");
                }

                // The turn is buffered end to end before anything streams, so it occupies a
                // connection and an agent slot for tens of seconds. Without a concurrency bound,
                // one customer with several tabs open monopolises the service while staying
                // comfortably inside the per-minute allowance above.
                return RateLimitPartition.GetConcurrencyLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = options.ConcurrentTurns,
                        QueueLimit = 0,
                    });
            });

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = options.RetryAfterSeconds.ToString();
                context.HttpContext.Response.ContentType = "application/json";

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitingExtensions));

                logger.LogWarning(
                    "Chat rate limit rejected a request from {Partition}.", ResolvePartitionKey(context.HttpContext));

                // Deliberately not ResultModel<T>. The SPA reads `message` off the error body
                // (src/UI/mango-ui/src/api/chat.ts), whereas ResultModel serialises `errorMessage`
                // — so the repo-conventional shape would surface as "Failed to send message" and
                // hide the actual reason from the customer.
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        message = options.RejectionMessage,
                        retryAfterSeconds = options.RetryAfterSeconds,
                    }),
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Partitions by customer, falling back to the connection.
    /// </summary>
    /// <remarks>
    /// Reads the claim straight off <c>HttpContext.User</c> rather than through
    /// <c>ICurrentUserContext</c>: the rate limiter middleware runs before
    /// <c>UseCurrentUserContext</c>, so the context object is not populated yet.
    /// </remarks>
    private static string ResolvePartitionKey(HttpContext httpContext)
        => httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.Identity?.Name
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
}
