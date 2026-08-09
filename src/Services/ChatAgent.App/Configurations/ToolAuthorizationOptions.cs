namespace ChatAgent.App.Configurations;

/// <summary>
/// Rules applied to a state-changing tool call before it is allowed to run.
/// </summary>
public class ToolAuthorizationOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Write tools the agent may currently invoke. An operational kill-switch: removing a name
    /// here disables that capability without a deploy.
    /// </summary>
    public string[] EnabledWriteTools { get; set; } =
        ["AddProductAsync", "ApplyCouponAsync", "RemoveCouponAsync"];

    /// <summary>Largest quantity a single add-to-cart call may request.</summary>
    public int MaxQuantityPerAdd { get; set; } = 20;

    /// <summary>Most state-changing calls one turn may make.</summary>
    public int MaxWritesPerTurn { get; set; } = 3;

    /// <summary>Shape a coupon code must have before it is forwarded anywhere.</summary>
    public string CouponCodePattern { get; set; } = "^[A-Za-z0-9_-]{3,32}$";

    /// <summary>
    /// Whether an add-to-cart is refused when the replicated stock is lower than the quantity.
    /// Has no effect while stock is unknown (null) for a product.
    /// </summary>
    public bool RequireStock { get; set; } = true;
}
