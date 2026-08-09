using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Mango.RestApis.Requests;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts;

public class UpsertCart
{
    /// <summary>
    /// Ceiling on a single cart line, matching the range the web DTO already advertised but never
    /// enforced.
    /// </summary>
    public const int MaxLineQuantity = 100;

    public class Command : ICommand<bool>
    {
        public required AddToCartRequestDto Cart { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            // DependentRules because Cart is `required` but can still arrive null from JSON, and
            // a rule reading x.Cart.Count would throw rather than fail validation.
            RuleFor(x => x.Cart).NotNull().DependentRules(() =>
            {
                RuleFor(x => x.Cart.ProductId).NotEmpty();

                // Previously unbounded: zero, negative and int.MaxValue all passed, and the
                // handler's `Count +=` could drive an existing line negative.
                RuleFor(x => x.Cart.Count).InclusiveBetween(1, MaxLineQuantity);

                RuleFor(x => x.Cart.CouponCode).MaximumLength(50);
            });
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
                    // Coupons are applied through ApplyCoupon, which verifies the code exists.
                    // Honouring one here was a way to store an unverified code by a side door.
                    CouponCode = string.Empty
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
                    // The validator bounds one request; it cannot see existing state, so the
                    // cumulative total has to be checked here. Both caps together make integer
                    // overflow unreachable.
                    var newCount = existingDetails.Count + request.Cart.Count;
                    if (newCount > MaxLineQuantity)
                    {
                        throw new DataVerificationException(
                            $"A cart line is limited to {MaxLineQuantity} items.");
                    }

                    existingDetails.Count = newCount;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ResultModel<bool>.Create(true);
        }
    }
}
