using FluentValidation;
using Mango.Core.Domain;
using Mango.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Dtos;
using ShoppingCart.API.Entities;

namespace ShoppingCart.API.Features.Carts;

public class UpsertCart
{
    public class Command : ICommand<CartDto>
    {
        public required CartDto Cart { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Cart).NotNull();
            RuleFor(x => x.Cart.CartHeader).NotNull();
            RuleFor(x => x.Cart.CartHeader.UserId).NotEmpty();
            RuleFor(x => x.Cart.CartDetails).NotEmpty();
        }
    }

    internal class Handler(ShoppingCartDbContext dbContext) : IRequestHandler<Command, ResultModel<CartDto>>
    {
        public async Task<ResultModel<CartDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var cartDto = request.Cart;
            var cartDetailsDto = cartDto.CartDetails.FirstOrDefault()
                ?? throw new DataVerificationException("Cart details are required");

            // 1. Ensure product exists in our local cache/table
            var product = await dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == cartDetailsDto.ProductId, cancellationToken);

            if (product == null)
            {
                product = new Product
                {
                    Id = cartDetailsDto.ProductId,
                    Name = string.Empty,
                    CategoryName    = string.Empty,
                    Description = string.Empty,
                    ImageUrl = string.Empty,
                    Price = 0,
                };

                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // 2. Check for existing CartHeader
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(h => h.UserId == cartDto.CartHeader.UserId, cancellationToken);

            if (cartHeader == null)
            {
                // Create new header and details
                cartHeader = new CartHeader
                {
                    UserId = cartDto.CartHeader.UserId,
                    CouponCode = cartDto.CartHeader.CouponCode
                };
                dbContext.CartHeaders.Add(cartHeader);
                await dbContext.SaveChangesAsync(cancellationToken);

                var newDetails = new CartDetails
                {
                    CartHeaderId = cartHeader.Id,
                    ProductId = product.Id,
                    Count = cartDetailsDto.Count
                };
                dbContext.CartDetails.Add(newDetails);
            }
            else
            {
                // Update existing header if needed (e.g. coupon)
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    cartHeader.CouponCode = cartDto.CartHeader.CouponCode;
                }

                // Check if product already in cart
                var existingDetails = await dbContext.CartDetails
                    .FirstOrDefaultAsync(d => d.CartHeaderId == cartHeader.Id && d.ProductId == product.Id, cancellationToken);

                if (existingDetails == null)
                {
                    var newDetails = new CartDetails
                    {
                        CartHeaderId = cartHeader.Id,
                        ProductId = product.Id,
                        Count = cartDetailsDto.Count
                    };
                    dbContext.CartDetails.Add(newDetails);
                }
                else
                {
                    existingDetails.Count += cartDetailsDto.Count;
                    dbContext.CartDetails.Update(existingDetails);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return ResultModel<CartDto>.Create(cartDto);
        }
    }
}
