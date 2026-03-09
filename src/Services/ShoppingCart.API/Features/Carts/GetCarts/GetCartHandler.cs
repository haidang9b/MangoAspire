using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Features.Carts.GetCarts;


public class GetCartHandler
{
    public class Validator : AbstractValidator<GetCardQuery>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext) : IRequestHandler<GetCardQuery, ResultModel<GetCardResponse>>
    {
        public async Task<ResultModel<GetCardResponse>> HandleAsync(GetCardQuery request, CancellationToken cancellationToken)
        {
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
}
