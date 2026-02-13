namespace ChatAgent.App.Plugins;

public class CheckoutPlugin : ICheckoutPlugin
{
    [KernelFunction]
    [Description("Handle checkout request from user")]
    public Task<string> CheckoutAsync()
    {
        return Task.FromResult("Please manual checkout");
    }
}
