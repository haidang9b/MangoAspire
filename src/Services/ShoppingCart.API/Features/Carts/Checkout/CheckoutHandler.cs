using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Mango.Events.Orders;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Features.Carts.Checkout.Checkout;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Features.Carts.Checkout;

public class CheckoutHandler(
        ShoppingCartDbContext dbContext,
        ICouponsApi couponsApi,
        IEventBus eventBus,
        ICurrentUserContext currentUser
    ) : IRequestHandler<CheckoutDto, ResultModel<bool>>
{
    public async Task<ResultModel<bool>> Handle(CheckoutDto request, CancellationToken cancellationToken)
    {
        var checkOutHeader = request.CheckoutRequestDto;

        // 1. Get current cart
        var cartHeader = await dbContext.CartHeaders
            .Include(u => u.CartDetails)
            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId, cancellationToken)
            ?? throw new DataVerificationException("Cart not found");

        // 2. Verify coupon if present
        if (!string.IsNullOrEmpty(checkOutHeader.CouponCode))
        {
            var couponDto = await couponsApi.GetCouponAsync(checkOutHeader.CouponCode);
            if (couponDto.Data != null && checkOutHeader.DiscountTotal != couponDto.Data.DiscountAmount)
            {
                throw new DataVerificationException("Coupon price has changed, please confirm");
            }
        }

        // 3. Send message to checkout queue (logic placeholder)
        // TODO: Inject IEventBus or similar and publish message
        var checkedOutEvent = new CartCheckedOutEvent
        {
            UserId = cartHeader.UserId,
            CartId = cartHeader.Id,
            FirstName = checkOutHeader.FirstName,
            LastName = checkOutHeader.LastName,
            Email = checkOutHeader.Email,
            Phone = checkOutHeader.Phone,
            CouponCode = checkOutHeader.CouponCode ?? string.Empty,
            CardNumber = checkOutHeader.CardNumber,
            CVV = checkOutHeader.CVV,
            DiscountTotal = checkOutHeader.DiscountTotal,
            ExpiryMonthYear = checkOutHeader.ExpiryMonthYear,
            OrderTotal = checkOutHeader.OrderTotal,
            PickupDate = checkOutHeader.PickupDate,
            CartTotalItems = cartHeader.CartDetails.Sum(x => x.Count),
            CartDetails = cartHeader.CartDetails.Select(d => new CartCheckedOutEvent.CartDetailsDto
            {
                Id = d.Id,
                CartHeaderId = d.CartHeaderId,
                Count = d.Count,
                ProductId = d.ProductId,

            }).ToList()
        };

        await eventBus.PublishAsync(checkedOutEvent);

        // 5. Clear cart
        dbContext.CartHeaders.Remove(cartHeader);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ResultModel<bool>.Create(true);
    }
}
