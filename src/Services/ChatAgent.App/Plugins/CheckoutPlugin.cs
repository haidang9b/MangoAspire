namespace ChatAgent.App.Plugins;

public class CheckoutPlugin : ICheckoutPlugin
{
    [KernelFunction]
    [Description("Guide the user to complete their order checkout. Use this when the user is ready to finalize their purchase, wants to check out, or asks to complete their order. This will direct them to the manual checkout process.")]
    public Task<string> CheckoutAsync()
    {
        return Task.FromResult("Please manual checkout");
    }
}
