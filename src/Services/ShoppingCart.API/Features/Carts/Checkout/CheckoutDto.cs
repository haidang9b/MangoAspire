using Mango.RestApis.Requests;

namespace ShoppingCart.API.Features.Carts.Checkout;

public class CheckoutDto : ICommand<bool>
{
    public required CheckoutRequestDto CheckoutRequestDto { get; set; }
}
