namespace Mango.Core.Options;

/// <summary>
/// Configuration options for service URLs.
/// Used for service-to-service communication.
/// </summary>
public class ServiceUrlsOptions
{
    public const string SectionName = "ServiceUrls";

    public string ProductsApi { get; set; } = "https://products-api";
    public string ShoppingCartApi { get; set; } = "https://shoppingcart-api";
    public string CouponsApi { get; set; } = "https://coupons-api";
    public string IdentityApp { get; set; } = "https://identity-app";
    public string OrdersApi { get; set; } = "https://orders-api";
    public string ChatAgentApp { get; set; } = "https://chatagent-app";
}
