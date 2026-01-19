using FluentValidation;
using Mango.Core.Domain;
using Mango.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;

namespace ShoppingCart.API.Features.Carts;

public class RemoveCoupon
{
    public class Command : ICommand<bool>
    {
        public required string UserId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(h => h.UserId == request.UserId, cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            cartHeader.CouponCode = "";
            dbContext.CartHeaders.Update(cartHeader);

            await dbContext.SaveChangesAsync(cancellationToken);
            return ResultModel<bool>.Create(true);
        }
    }
}
