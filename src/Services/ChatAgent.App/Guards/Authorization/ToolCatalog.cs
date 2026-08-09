using System.Collections.Frozen;

namespace ChatAgent.App.Guards.Authorization;

/// <summary>
/// The kernel functions this agent exposes, and which of them change state.
/// </summary>
/// <remarks>
/// Maintained by hand alongside <c>Plugins/*.cs</c>, and kept honest by
/// <c>ToolCatalogTests</c>, which reflects over the plugin classes and fails when a
/// <see cref="KernelFunctionAttribute"/> appears here that is not listed, or vice versa. A new
/// tool that nobody adds to <see cref="WriteFunctions"/> would otherwise skip authorization
/// silently, which is the failure this catalogue exists to prevent.
/// </remarks>
public static class ToolCatalog
{
    /// <summary>
    /// Functions that mutate state and therefore require authorization before they run.
    /// </summary>
    public static readonly FrozenSet<string> WriteFunctions = new[]
    {
        "AddProductAsync",
        "ApplyCouponAsync",
        "RemoveCouponAsync",
        "CheckoutAsync",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Every exposed function name. Also used by the answer fact checker: a draft that names one
    /// of these is leaking internals, whatever else it got right.
    /// </summary>
    public static readonly FrozenSet<string> AllFunctionNames = new[]
    {
        // CartPlugin
        "ApplyCouponAsync",
        "RemoveCouponAsync",
        "AddProductAsync",
        "GetCurrentCartAsync",
        // CheckoutPlugin
        "CheckoutAsync",
        // CouponsPlugin
        "GetCouponAsync",
        // ProductsPlugin
        "GetAllProductsAsync",
        "SearchProductsAsync",
        "GetProductByIdAsync",
        "GetCategoriesAsync",
        "SearchStoreInfoAsync",
        // WebSearchPlugin
        "SearchWebAsync",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsWrite(string? functionName)
        => !string.IsNullOrEmpty(functionName) && WriteFunctions.Contains(functionName);
}
