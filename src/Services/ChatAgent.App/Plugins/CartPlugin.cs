using Mango.Core.Auth;

namespace ChatAgent.App.Plugins;

public class CartPlugin : ICartPlugin
{
    private readonly ICartApi _cartApi;

    private readonly ICurrentUserContext _currentUserContext;

    public CartPlugin(ICartApi cartApi, ICurrentUserContext currentUserContext)
    {
        _cartApi = cartApi;
        _currentUserContext = currentUserContext;
    }

    [KernelFunction]
    [Description("Apply a discount coupon code to the user's shopping cart. Use this when the user provides a coupon or discount code they want to use.")]
    public async Task<bool> ApplyCouponAsync([Description("The coupon or discount code to apply")] string code)
    {
        var result = await _cartApi.ApplyCouponAsync(new ApplyCouponRequestDto
        {
            CouponCode = code
        });

        return result.Data;
    }

    [KernelFunction]
    [Description("Remove the currently applied coupon from the user's cart. Use this when the user wants to remove or change their discount code.")]
    public async Task<bool> RemoveCouponAsync()
    {
        var result = await _cartApi.RemoveCouponAsync();

        return result.Data;
    }

    [KernelFunction]
    [Description("Add a product to the user's shopping cart with the specified quantity. Use this when the user wants to order or add items to their cart.")]
    public async Task<bool> AddProductAsync(
        [Description("The unique identifier (GUID) of the product to add")] Guid productId,
        [Description("The number of items to add (must be greater than 0)")] int quantity)
    {
        var result = await _cartApi.AddToCartAsync(new AddToCartRequestDto
        {
            ProductId = productId,
            Count = quantity
        });

        return result.Data;
    }

    [KernelFunction]
    [Description("Retrieve the current user's shopping cart with all items, quantities, prices, and applied coupons. Use this to show what's in the cart or calculate totals.")]
    public async Task<CartDto?> GetCurrentCartAsync()
    {
        var result = await _cartApi.GetCartByUserIdAsync(_currentUserContext.UserId!);

        return result.Data;
    }
}
