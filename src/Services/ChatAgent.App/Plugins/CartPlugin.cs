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
    [Description("Apply coupon to cart")]
    public async Task<bool> ApplyCouponAsync([Description("Counpon code")] string code)
    {
        var result = await _cartApi.ApplyCouponAsync(new ApplyCouponRequestDto
        {
            CouponCode = code
        });

        return result.Data;
    }

    [KernelFunction]
    [Description("Remove coupon from cart")]
    public async Task<bool> RemoveCouponAsync()
    {
        var result = await _cartApi.RemoveCouponAsync();

        return result.Data;
    }

    [KernelFunction]
    [Description("Add product to cart")]
    public async Task<bool> AddProductAsync([Description("Product id")] Guid productId, [Description("Quantity")] int quantity)
    {
        var result = await _cartApi.AddToCartAsync(new AddToCartRequestDto
        {
            ProductId = productId,
            Count = quantity
        });

        return result.Data;
    }

    [KernelFunction]
    [Description("Get cart of current user")]
    public async Task<CartDto?> GetCurrentCartAsync()
    {
        var result = await _cartApi.GetCartByUserIdAsync(_currentUserContext.UserId!);

        return result.Data;
    }
}
