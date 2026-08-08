using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Mango.RestApis.Requests;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts;

public class UpsertCart
{
    public class Command : ICommand<bool>
    {
        public required AddToCartRequestDto Cart { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Cart).NotNull();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext, ICurrentUserContext currentUser) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            // Products are a local replica kept in sync by CDC. Check the
            // product is present before inserting, so a product this service
            // has not replicated yet surfaces as a business error rather than
            // a foreign key violation from SaveChangesAsync.
            // Upstream deletes tombstone the row instead of removing it (the replay fence
            // needs the LSN watermark), so a delisted product must be excluded explicitly.
            var productExists = await dbContext.Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.Cart.ProductId && !p.IsDeleted, cancellationToken);

            if (!productExists)
            {
                throw new DataVerificationException($"Product '{request.Cart.ProductId}' was not found.");
            }

            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(h => h.UserId == currentUser.UserId, cancellationToken);


            if (cartHeader == null)
            {
                cartHeader = new CartHeader
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    CouponCode = request.Cart.CouponCode
                };

                await dbContext.CartHeaders.AddAsync(cartHeader, cancellationToken);

                var newDetails = new CartDetails
                {
                    CartHeaderId = cartHeader.Id,
                    ProductId = request.Cart.ProductId,
                    Count = request.Cart.Count
                };
                await dbContext.CartDetails.AddAsync(newDetails);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Cart.CouponCode))
                {
                    cartHeader.CouponCode = request.Cart.CouponCode;
                }

                var existingDetails = await dbContext.CartDetails
                    .FirstOrDefaultAsync(d => d.CartHeaderId == cartHeader.Id && d.ProductId == request.Cart.ProductId, cancellationToken);

                if (existingDetails == null)
                {
                    var newDetails = new CartDetails
                    {
                        CartHeaderId = cartHeader.Id,
                        ProductId = request.Cart.ProductId,
                        Count = request.Cart.Count

                    };
                    await dbContext.CartDetails.AddAsync(newDetails);
                }
                else
                {
                    existingDetails.Count += request.Cart.Count;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ResultModel<bool>.Create(true);
        }
    }
}
