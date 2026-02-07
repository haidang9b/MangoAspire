using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts;

public class RemoveCoupon
{
    public class Command : ICommand<bool>
    {
    }

    internal class Handler(ShoppingCartDbContext dbContext, ICurrentUserContext currentUser) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(h => h.UserId == currentUser.UserId, cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            cartHeader.CouponCode = "";
            dbContext.CartHeaders.Update(cartHeader);

            await dbContext.SaveChangesAsync(cancellationToken);
            return ResultModel<bool>.Create(true);
        }
    }
}
