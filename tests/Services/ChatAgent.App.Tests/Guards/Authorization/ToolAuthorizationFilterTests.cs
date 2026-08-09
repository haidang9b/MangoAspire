using ChatAgent.App.Configurations;
using ChatAgent.App.Guards.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using Shouldly;

namespace ChatAgent.App.Tests.Guards.Authorization;

/// <summary>
/// Exercises the filter through a real <see cref="Kernel"/>, because
/// <see cref="FunctionInvocationContext"/> cannot be constructed directly.
/// </summary>
/// <remarks>
/// The assertion that matters throughout is <c>wasInvoked.ShouldBeFalse()</c>. Everything else
/// the filter does is reporting; not running the function is the security property.
/// </remarks>
public class ToolAuthorizationFilterTests
{
    private static ToolAuthorizationFilter CreateFilter(
        Mock<IToolAuthorizer> authorizer,
        ToolAuthorizationOptions? options = null)
        => new(
            authorizer.Object,
            Options.Create(new AIAgentConfiguration { ToolAuthorization = options ?? new ToolAuthorizationOptions() }),
            NullLogger<ToolAuthorizationFilter>.Instance);

    private static Mock<IToolAuthorizer> Authorizer(ToolAuthorizationDecision decision)
    {
        var mock = new Mock<IToolAuthorizer>();
        mock.Setup(x => x.AuthorizeAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        return mock;
    }

    private static (Kernel Kernel, Func<bool> WasInvoked) BuildKernel(
        ToolAuthorizationFilter filter,
        string functionName)
    {
        var invoked = false;

        var function = KernelFunctionFactory.CreateFromMethod(
            (Guid productId, int quantity) =>
            {
                invoked = true;
                return "added";
            },
            functionName);

        var kernel = Kernel.CreateBuilder().Build();
        kernel.Plugins.AddFromFunctions("cart", [function]);
        kernel.FunctionInvocationFilters.Add(filter);

        return (kernel, () => invoked);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_When_AuthorizerDenies_Then_TheFunctionIsNeverInvoked()
    {
        var authorizer = Authorizer(ToolAuthorizationDecision.Deny("product-not-found", "No such dish."));
        var (kernel, wasInvoked) = BuildKernel(CreateFilter(authorizer), "AddProductAsync");

        await kernel.InvokeAsync(
            kernel.Plugins["cart"]["AddProductAsync"],
            new KernelArguments { ["productId"] = Guid.NewGuid(), ["quantity"] = 1 });

        wasInvoked().ShouldBeFalse();
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_When_AuthorizerDenies_Then_ResultDescribesTheDenial()
    {
        var authorizer = Authorizer(ToolAuthorizationDecision.Deny("insufficient-stock", "Only 2 left."));
        var (kernel, _) = BuildKernel(CreateFilter(authorizer), "AddProductAsync");

        var result = await kernel.InvokeAsync(
            kernel.Plugins["cart"]["AddProductAsync"],
            new KernelArguments { ["productId"] = Guid.NewGuid(), ["quantity"] = 5 });

        var text = result.GetValue<string>();
        text.ShouldNotBeNull();
        text.ShouldContain("denied");
        // The rule id stays in the logs; the model is told only what it can repeat to a customer.
        text.ShouldContain("Only 2 left.");
        text.ShouldNotContain("insufficient-stock");
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_When_AuthorizerAllows_Then_TheFunctionRuns()
    {
        var authorizer = Authorizer(ToolAuthorizationDecision.Allow);
        var (kernel, wasInvoked) = BuildKernel(CreateFilter(authorizer), "AddProductAsync");

        await kernel.InvokeAsync(
            kernel.Plugins["cart"]["AddProductAsync"],
            new KernelArguments { ["productId"] = Guid.NewGuid(), ["quantity"] = 1 });

        wasInvoked().ShouldBeTrue();
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_When_FilterIsDisabled_Then_TheAuthorizerIsNeverConsulted()
    {
        var authorizer = Authorizer(ToolAuthorizationDecision.Deny("tool-disabled", "no"));
        var filter = CreateFilter(authorizer, new ToolAuthorizationOptions { Enabled = false });
        var (kernel, wasInvoked) = BuildKernel(filter, "AddProductAsync");

        await kernel.InvokeAsync(
            kernel.Plugins["cart"]["AddProductAsync"],
            new KernelArguments { ["productId"] = Guid.NewGuid(), ["quantity"] = 1 });

        wasInvoked().ShouldBeTrue();
        authorizer.Verify(
            x => x.AuthorizeAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_When_Invoked_Then_ForwardsTheFunctionNameAndArguments()
    {
        var authorizer = Authorizer(ToolAuthorizationDecision.Allow);
        var (kernel, _) = BuildKernel(CreateFilter(authorizer), "AddProductAsync");
        var productId = Guid.NewGuid();

        await kernel.InvokeAsync(
            kernel.Plugins["cart"]["AddProductAsync"],
            new KernelArguments { ["productId"] = productId, ["quantity"] = 3 });

        authorizer.Verify(
            x => x.AuthorizeAsync(
                "AddProductAsync",
                It.Is<IReadOnlyDictionary<string, object?>>(a =>
                    a["productId"]!.Equals(productId) && a["quantity"]!.Equals(3)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
