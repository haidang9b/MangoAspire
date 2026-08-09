namespace ChatAgent.App.Configurations;

/// <summary>
/// Transport settings for the Azure OpenAI client.
/// </summary>
/// <remarks>
/// The client is constructed by hand rather than resolved from <c>IHttpClientFactory</c>, which
/// means the standard resilience handler configured by <c>AddServiceDefaults</c> never applied to
/// it: model calls had no timeout, no retry policy and no circuit breaker, and a wedged request
/// hung the customer's turn for as long as they stayed connected.
/// </remarks>
public class AgentHttpOptions
{
    /// <summary>Total budget for one model request including retries.</summary>
    public int TotalTimeoutSeconds { get; set; } = 120;

    /// <summary>Budget for a single attempt.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 60;

    public int MaxRetries { get; set; } = 2;
}
