using FluentValidation;
using Mango.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;

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
        public async Task<ResultModel<GetCardResponse>> Handle(GetCardQuery request, CancellationToken cancellationToken)
        {
            var cartHeaderDto = await dbContext.CartHeaders
                .AsNoTracking()
                .Where(u => u.UserId == request.UserId)
                .Select(u => new GetCardResponse.CartHeaderResponseDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    CouponCode = u.CouponCode
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (cartHeaderDto == null)
            {
                return ResultModel<GetCardResponse>.Create(null);
            }

            var cartDetailsDto = await dbContext.CartDetails
               .AsNoTracking()
               .Where(u => u.CartHeaderId == cartHeaderDto.Id)
               .Select(d => new GetCardResponse.CartDetailsResponseDto
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
               })
               .ToListAsync(cancellationToken);

            var cartDto = new GetCardResponse
            {
                CartHeader = cartHeaderDto,
                CartDetails = cartDetailsDto
            };

            return ResultModel<GetCardResponse>.Create(cartDto);
        }
    }
}
