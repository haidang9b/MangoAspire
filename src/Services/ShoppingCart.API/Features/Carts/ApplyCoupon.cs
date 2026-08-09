using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Refit;
using ShoppingCart.API.Dtos;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Features.Carts;

public class ApplyCoupon
{
    public class Command : ICommand<bool>
    {
        public required string CouponCode { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CouponCode).NotEmpty().MaximumLength(50);
        }
    }

    internal class Handler(
        ShoppingCartDbContext dbContext,
        ICouponsApi couponsApi,
        ICurrentUserContext currentUser) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(h => h.UserId == currentUser.UserId, cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            var coupon = await GetCouponAsync(request.CouponCode);

            // Coupons.API answers 200 with a null payload for a code it does not know, so a call
            // that did not throw is not evidence the coupon exists. Any string used to be stored
            // verbatim and only questioned at checkout.
            if (coupon?.Data is null)
            {
                throw new DataVerificationException($"Coupon '{request.CouponCode}' is not valid.");
            }

            // The canonical code from the source, so casing cannot drift from what checkout looks up.
            cartHeader.CouponCode = coupon.Data.Code;
            dbContext.CartHeaders.Update(cartHeader);

            await dbContext.SaveChangesAsync(cancellationToken);
            return ResultModel<bool>.Create(true);
        }

        private async Task<ResultModel<CouponDto>?> GetCouponAsync(string couponCode)
        {
            try
            {
                return await couponsApi.GetCouponAsync(couponCode);
            }
            catch (ApiException ex)
            {
                // Fail closed. Applying only records an intent and checkout re-verifies, but
                // treating an unreachable coupon service as "probably fine" reopens exactly the
                // hole this check exists to close.
                throw new DataVerificationException("Could not verify that coupon right now.", ex);
            }
        }
    }
}
