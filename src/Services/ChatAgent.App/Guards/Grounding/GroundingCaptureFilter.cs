using ChatAgent.App.Configurations;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChatAgent.App.Guards.Grounding;

/// <summary>
/// Records every auto-invoked tool result into the request's <see cref="IGroundingContext"/>
/// and caps how many tool round-trips a single turn may make.
/// </summary>
/// <remarks>
/// Two jobs, both of which Semantic Kernel leaves to the host. The capture gives the
/// response guard concrete facts to check the draft against — without it, verification
/// would only be a second opinion from the same model. The iteration cap closes an
/// unbounded loop: automatic function calling will otherwise keep invoking tools for as
/// long as the model asks.
/// </remarks>
public class GroundingCaptureFilter : IAutoFunctionInvocationFilter
{
    private readonly IGroundingContext _groundingContext;
    private readonly GuardOptions _options;
    private readonly ILogger<GroundingCaptureFilter> _logger;

    public GroundingCaptureFilter(
        IGroundingContext groundingContext,
        IOptions<AIAgentConfiguration> options,
        ILogger<GroundingCaptureFilter> logger)
    {
        _groundingContext = groundingContext;
        _options = options.Value.Guard;
        _logger = logger;
    }

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        await next(context);

        _groundingContext.Record(
            context.Function.Name,
            Stringify(context.Result.GetValue<object>()));

        // RequestSequenceIndex is zero-based, so this stops the loop once the configured
        // number of round-trips has been made.
        if (context.RequestSequenceIndex >= _options.MaxToolIterations - 1)
        {
            _logger.LogWarning(
                "Tool iteration cap of {Max} reached; terminating automatic function calling.",
                _options.MaxToolIterations);

            context.Terminate = true;
        }
    }

    private static string? Stringify(object? value) => value switch
    {
        null => null,
        string text => text,
        _ => TrySerialize(value),
    };

    private static string? TrySerialize(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch (NotSupportedException)
        {
            // A tool returned something non-serialisable. Grounding is best-effort — the
            // guard simply has one less fact to check rather than the turn failing.
            return value.ToString();
        }
    }
}
