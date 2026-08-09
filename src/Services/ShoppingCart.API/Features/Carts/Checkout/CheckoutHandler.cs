using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Mango.Events.Orders;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Features.Carts.Checkout;

public class CheckoutHandler(
        ShoppingCartDbContext dbContext,
        ICouponsApi couponsApi,
        IEventBus eventBus,
        ICurrentUserContext currentUser,
        ILogger<CheckoutHandler> logger
    ) : IRequestHandler<CheckoutDto, ResultModel<bool>>
{
    /// <summary>Tolerance when comparing the client's total against the server's.</summary>
    /// <remarks>
    /// The SPA computes its total in JavaScript, so it arrives via IEEE-754 while the server works
    /// in decimal - 18.99 x 3 can legitimately differ by a cent.
    /// </remarks>
    private const decimal TotalComparisonTolerance = 0.01m;

    public async Task<ResultModel<bool>> HandleAsync(CheckoutDto request, CancellationToken cancellationToken)
    {
        var checkOutHeader = request.CheckoutRequestDto;

        // 1. Get current cart. Products are included because prices come from the replicated
        // catalogue, never from the request.
        var cartHeader = await dbContext.CartHeaders
            .Include(u => u.CartDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId, cancellationToken)
            ?? throw new DataVerificationException("Cart not found");

        if (cartHeader.CartDetails.Count == 0)
        {
            throw new DataVerificationException("Cart is empty");
        }

        var subtotal = cartHeader.CartDetails.Sum(d => d.Product.Price * d.Count);

        // 2. The coupon is whatever the cart holds, not whatever the request claims. Reading it
        // from the request let a client omit the code and skip verification altogether.
        var couponCode = cartHeader.CouponCode;
        decimal discountTotal = 0m;

        if (!string.IsNullOrEmpty(couponCode))
        {
            var couponDto = await couponsApi.GetCouponAsync(couponCode);

            // A null payload means the coupon was withdrawn since it was applied. This branch
            // previously fell through, and the client's own DiscountTotal was published unchecked.
            if (couponDto?.Data is null)
            {
                throw new DataVerificationException(
                    "That coupon is no longer available, please review your order.");
            }

            discountTotal = couponDto.Data.DiscountAmount;
        }

        var orderTotal = Math.Max(0m, subtotal - discountTotal);

        // Logged rather than rejected for now. The published totals are already the server's, so
        // the security objective is met; promoting this to a hard failure needs the client-side
        // rounding differences to be quiet first.
        if (Math.Abs(checkOutHeader.OrderTotal - orderTotal) > TotalComparisonTolerance)
        {
            logger.LogWarning(
                "Checkout total mismatch for user {UserId}: client sent {ClientTotal}, server computed {ServerTotal}.",
                cartHeader.UserId, checkOutHeader.OrderTotal, orderTotal);
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
            // Server-computed values, not the client's. Everything downstream — the orchestrator,
            // Orders and Payments — treats this event as authoritative.
            CouponCode = couponCode ?? string.Empty,
            CardNumber = checkOutHeader.CardNumber,
            CVV = checkOutHeader.CVV,
            DiscountTotal = discountTotal,
            ExpiryMonthYear = checkOutHeader.ExpiryMonthYear,
            OrderTotal = orderTotal,
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
