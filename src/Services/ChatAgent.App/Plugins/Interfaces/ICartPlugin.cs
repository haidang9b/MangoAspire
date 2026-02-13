namespace ChatAgent.App.Plugins.Interfaces;

public interface ICartPlugin
{
    Task<bool> ApplyCouponAsync(string code);
    Task<bool> AddProductAsync(Guid productId, int count);
    Task<CartDto?> GetCurrentCartAsync();
}
