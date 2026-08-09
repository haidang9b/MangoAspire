using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChatAgent.App.Guards.Authorization;

/// <summary>
/// Pre-execution authorization for every tool call the agent makes.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately an <see cref="IFunctionInvocationFilter"/> and not an
/// <see cref="IAutoFunctionInvocationFilter"/>. The existing
/// <see cref="Grounding.GroundingCaptureFilter"/> is the latter and calls <c>next</c> first,
/// which is correct for recording a result and useless for preventing one: by the time it runs,
/// the cart has already been written to.
/// </para>
/// <para>
/// Here, a denial simply does not call <c>next</c>. The function never executes, no HTTP request
/// reaches the downstream service, and the model is handed a refusal it can explain to the
/// customer. That is the whole mechanism - there is no flag to forget to check.
/// </para>
/// </remarks>
public class ToolAuthorizationFilter : IFunctionInvocationFilter
{
    private readonly IToolAuthorizer _authorizer;
    private readonly ToolAuthorizationOptions _options;
    private readonly ILogger<ToolAuthorizationFilter> _logger;

    public ToolAuthorizationFilter(
        IToolAuthorizer authorizer,
        IOptions<AIAgentConfiguration> options,
        ILogger<ToolAuthorizationFilter> logger)
    {
        _authorizer = authorizer;
        _options = options.Value.ToolAuthorization;
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        if (!_options.Enabled)
        {
            await next(context);
            return;
        }

        var arguments = context.Arguments.ToDictionary(
            argument => argument.Key,
            argument => argument.Value,
            StringComparer.OrdinalIgnoreCase);

        var decision = await _authorizer.AuthorizeAsync(
            context.Function.Name, arguments, context.CancellationToken);

        if (decision.Allowed)
        {
            await next(context);
            return;
        }

        _logger.LogWarning(
            "Tool {Tool} denied by rule {RuleId}.", context.Function.Name, decision.RuleId);

        // next() is never called: this is the veto.
        context.Result = new FunctionResult(
            context.Function,
            JsonSerializer.Serialize(new { denied = true, reason = decision.Reason }));
    }
}
