namespace ChatAgent.App.Plugins.Interfaces;

public interface ICheckoutPlugin
{
    Task<string> CheckoutAsync();
}
