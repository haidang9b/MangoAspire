using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts.GetCarts;

public class Validator : AbstractValidator<GetCardQuery>
{
    public Validator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}


internal class GetCartHandler(ShoppingCartDbContext dbContext, ICurrentUserContext currentUser)
    : IRequestHandler<GetCardQuery, ResultModel<GetCardResponse>>
{
    public async Task<ResultModel<GetCardResponse>> HandleAsync(GetCardQuery request, CancellationToken cancellationToken)
    {
        // The user id stays in the route because three clients build the URL that way, but it is
        // only ever allowed to be the caller's own. Without this the endpoint served any cart to
        // anyone who could guess an id.
        if (!string.Equals(request.UserId, currentUser.UserId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You can only view your own cart.");
        }

        var cartDto = await dbContext.CartHeaders
            .AsNoTracking()
            .Where(u => u.UserId == request.UserId)
            .Select(u => new GetCardResponse
            {
                CartHeader = new GetCardResponse.CartHeaderResponseDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    CouponCode = u.CouponCode
                },
                CartDetails = u.CartDetails.Select(d => new GetCardResponse.CartDetailsResponseDto
                {

                    Id = d.Id,
                    CartHeaderId = d.CartHeaderId,
                    ProductId = d.ProductId,
                    Count = d.Count,
                    Product = new GetCardResponse.ProductResponseDto
                    {
                        Id = d.Product.Id,
                        Name = d.Product.Name,
                        Price = d.Product.Price,
                        Description = d.Product.Description,
                        CategoryName = d.Product.CategoryName,
                        ImageUrl = d.Product.ImageUrl
                    },
                    CartHeader = null!
                }).ToList()
            }
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (cartDto == null)
        {
            return ResultModel<GetCardResponse>.Create(null);
        }


        return ResultModel<GetCardResponse>.Create(cartDto);
    }
}

