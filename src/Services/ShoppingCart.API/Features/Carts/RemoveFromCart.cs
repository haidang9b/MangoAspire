using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts;

public class RemoveFromCart
{
    public class Command : ICommand<bool>
    {
        public Guid CartDetailsId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CartDetailsId).NotEmpty();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext, ICurrentUserContext currentUser)
        : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            // Ownership is part of the lookup rather than a check after it, so another customer's
            // line is indistinguishable from one that does not exist. Checking afterwards and
            // reporting "forbidden" would turn this into an oracle for valid cart-detail ids.
            var cartDetails = await dbContext.CartDetails
                .FirstOrDefaultAsync(
                    d => d.Id == request.CartDetailsId && d.CartHeader.UserId == currentUser.UserId,
                    cancellationToken)
                ?? throw new DataVerificationException("Cart item not found");

            int totalCountOfCartItems = await dbContext.CartDetails
                .CountAsync(d => d.CartHeaderId == cartDetails.CartHeaderId, cancellationToken);

            dbContext.CartDetails.Remove(cartDetails);

            if (totalCountOfCartItems == 1)
            {
                var cartHeader = await dbContext.CartHeaders
                    .FirstOrDefaultAsync(h => h.Id == cartDetails.CartHeaderId, cancellationToken);
                if (cartHeader != null)
                {
                    dbContext.CartHeaders.Remove(cartHeader);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return ResultModel<bool>.Create(true);
        }
    }
}
