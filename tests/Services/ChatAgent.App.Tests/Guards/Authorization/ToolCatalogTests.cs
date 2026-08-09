using ChatAgent.App.Guards.Authorization;
using ChatAgent.App.Plugins;
using Microsoft.SemanticKernel;
using Shouldly;
using System.Reflection;

namespace ChatAgent.App.Tests.Guards.Authorization;

/// <summary>
/// Keeps the hand-maintained catalogue honest against the plugins it describes.
/// </summary>
/// <remarks>
/// A new tool that nobody adds to <see cref="ToolCatalog"/> would skip authorization silently and
/// would be invisible to the answer fact checker's internal-leak rule. Reflecting over the plugin
/// classes turns that maintenance risk into a failing test.
/// </remarks>
public class ToolCatalogTests
{
    private static readonly Type[] PluginTypes =
    [
        typeof(CartPlugin),
        typeof(CheckoutPlugin),
        typeof(CouponsPlugin),
        typeof(ProductsPlugin),
        typeof(WebSearchPlugin),
    ];

    private static IEnumerable<string> DiscoverKernelFunctions()
        => PluginTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() is not null)
            .Select(m => m.Name);

    [Fact]
    public void AllFunctionNames_When_ComparedToThePlugins_Then_ContainsEveryKernelFunction()
    {
        var missing = DiscoverKernelFunctions()
            .Where(name => !ToolCatalog.AllFunctionNames.Contains(name))
            .ToList();

        missing.ShouldBeEmpty(
            $"These [KernelFunction] methods are not in ToolCatalog.AllFunctionNames: {string.Join(", ", missing)}");
    }

    [Fact]
    public void AllFunctionNames_When_ComparedToThePlugins_Then_ListsNothingThatNoLongerExists()
    {
        var discovered = DiscoverKernelFunctions().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stale = ToolCatalog.AllFunctionNames.Where(name => !discovered.Contains(name)).ToList();

        stale.ShouldBeEmpty(
            $"ToolCatalog.AllFunctionNames lists functions that no plugin exposes: {string.Join(", ", stale)}");
    }

    [Fact]
    public void WriteFunctions_When_Listed_Then_AreAllRealFunctions()
    {
        var discovered = DiscoverKernelFunctions().ToHashSet(StringComparer.OrdinalIgnoreCase);

        ToolCatalog.WriteFunctions.ShouldAllBe(name => discovered.Contains(name));
    }

    [Theory]
    [InlineData("AddProductAsync")]
    [InlineData("ApplyCouponAsync")]
    [InlineData("CheckoutAsync")]
    public void IsWrite_When_FunctionChangesState_Then_IsTrue(string functionName)
        => ToolCatalog.IsWrite(functionName).ShouldBeTrue();

    [Theory]
    [InlineData("SearchProductsAsync")]
    [InlineData("GetAllProductsAsync")]
    [InlineData("SearchStoreInfoAsync")]
    [InlineData(null)]
    public void IsWrite_When_FunctionOnlyReads_Then_IsFalse(string? functionName)
        => ToolCatalog.IsWrite(functionName).ShouldBeFalse();
}
