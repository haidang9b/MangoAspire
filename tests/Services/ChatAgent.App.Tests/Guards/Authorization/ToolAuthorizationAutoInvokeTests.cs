using ChatAgent.App.Configurations;
using ChatAgent.App.Guards.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using Shouldly;

namespace ChatAgent.App.Tests.Guards.Authorization;

/// <summary>
/// Pins that <see cref="ToolAuthorizationFilter"/> vetoes on the path the agent actually uses.
/// </summary>
/// <remarks>
/// <para>
/// The filter is only worth anything if it runs during <b>automatic</b> function calling. That is
/// a different Semantic Kernel code path from a direct <c>kernel.InvokeAsync</c>: the model
/// requests a call, SK dispatches it internally, and the agent never invokes the function itself.
/// A filter that fired only on direct invocation would pass every test in
/// <see cref="ToolAuthorizationFilterTests"/> and stop nothing in production.
/// </para>
/// <para>
/// The call is therefore executed through the same API a connector uses once it has parsed a tool
/// call from the model - see the remarks on <c>RunAutoInvokeAsync</c>.
/// </para>
/// </remarks>
public class ToolAuthorizationAutoInvokeTests
{
    private static ToolAuthorizationFilter CreateFilter(ToolAuthorizationDecision decision)
    {
        var authorizer = new Mock<IToolAuthorizer>();
        authorizer.Setup(x => x.AuthorizeAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        return new ToolAuthorizationFilter(
            authorizer.Object,
            Options.Create(new AIAgentConfiguration()),
            NullLogger<ToolAuthorizationFilter>.Instance);
    }

    /// <summary>
    /// Executes the model's requested call the way a connector does.
    /// </summary>
    /// <remarks>
    /// Driving a real auto-invocation loop from a test is not possible here: the loop lives in the
    /// connector (the OpenAI one), not in a generic wrapper over <see cref="IChatCompletionService"/>,
    /// so a fake completion service that returns a <see cref="FunctionCallContent"/> is simply
    /// never acted upon. An earlier version of this test did exactly that, and its "deny" case
    /// passed while the tool was not running in either case - the assertion was vacuous.
    ///
    /// <see cref="FunctionCallContent.InvokeAsync(Kernel, CancellationToken)"/> is the API the
    /// connector calls once it has parsed a tool call, so this is the real seam: if the filter
    /// runs here, it runs during automatic function calling.
    /// </remarks>
    private static async Task<bool> RunAutoInvokeAsync(ToolAuthorizationDecision decision)
    {
        var invoked = false;

        var function = KernelFunctionFactory.CreateFromMethod(
            (Guid productId, int quantity) =>
            {
                invoked = true;
                return "added to cart";
            },
            "AddProductAsync");

        var arguments = new KernelArguments
        {
            ["productId"] = Guid.NewGuid(),
            ["quantity"] = 2,
        };

        var kernel = Kernel.CreateBuilder().Build();
        kernel.Plugins.AddFromFunctions("cart", [function]);
        kernel.FunctionInvocationFilters.Add(CreateFilter(decision));

        // What the model asked for, and what the connector turns it into.
        var call = new FunctionCallContent("AddProductAsync", "cart", "call-1", arguments);

        await call.InvokeAsync(kernel);

        return invoked;
    }

    [Fact]
    public async Task AutoFunctionCalling_When_AuthorizerDenies_Then_TheToolNeverRuns()
    {
        var invoked = await RunAutoInvokeAsync(
            ToolAuthorizationDecision.Deny("insufficient-stock", "Only 1 left."));

        // If this ever fails, the filter is not on the auto-invoke path and the entire
        // authorization stage is decorative - move it to an IAutoFunctionInvocationFilter
        // registered before GroundingCaptureFilter that sets Result and skips next().
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task AutoFunctionCalling_When_AuthorizerAllows_Then_TheToolRuns()
    {
        var invoked = await RunAutoInvokeAsync(ToolAuthorizationDecision.Allow);

        invoked.ShouldBeTrue();
    }
}
