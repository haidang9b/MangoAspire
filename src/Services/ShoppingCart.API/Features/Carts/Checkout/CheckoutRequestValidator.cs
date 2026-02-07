using ShoppingCart.API.Features.Carts.Checkout.Checkout;

namespace ShoppingCart.API.Features.Carts.Checkout;

public class CheckoutRequestValidator : AbstractValidator<CheckoutDto>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.CheckoutRequestDto.FirstName).NotEmpty();
        RuleFor(x => x.CheckoutRequestDto.LastName).NotEmpty();
        RuleFor(x => x.CheckoutRequestDto.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.CheckoutRequestDto.Phone).NotEmpty();
    }
}

