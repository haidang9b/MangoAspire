using FluentValidation;
using Mango.Core.Domain;
using Mango.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Dtos;

namespace ShoppingCart.API.Features.Carts;

public class GetCart
{
    public class Query : IQuery<CartDto>
    {
        public required string UserId { get; init; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext) : IRequestHandler<Query, ResultModel<CartDto>>
    {
        public async Task<ResultModel<CartDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cartHeaderDto = await dbContext.CartHeaders
                .AsNoTracking()
                .Where(u => u.UserId == request.UserId)
                .Select(u => new CartHeaderDto
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    CouponCode = u.CouponCode
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            var cartDetailsDto = await dbContext.CartDetails
                .AsNoTracking()
                .Where(u => u.CartHeaderId == cartHeaderDto.Id)
                .Select(d => new CartDetailsDto
                {
                    Id = d.Id,
                    CartHeaderId = d.CartHeaderId,
                    ProductId = d.ProductId,
                    Count = d.Count,
                    Product = new ProductDto
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

            var cartDto = new CartDto
            {
                CartHeader = cartHeaderDto,
                CartDetails = cartDetailsDto
            };

            return ResultModel<CartDto>.Create(cartDto);
        }
    }
}
