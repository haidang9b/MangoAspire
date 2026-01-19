using EventBus.Abstractions;
using FluentValidation;
using Mango.Core.Domain;
using Mango.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Dtos;
using ShoppingCart.API.IntegrationEvents;
using ShoppingCart.API.Services;

namespace ShoppingCart.API.Features.Carts;

public class Checkout
{
    public class Command : ICommand<bool>
    {
        public required CheckoutHeaderDto CheckoutHeader { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CheckoutHeader).NotNull();
            RuleFor(x => x.CheckoutHeader.UserId).NotEmpty();
            RuleFor(x => x.CheckoutHeader.FirstName).NotEmpty();
            RuleFor(x => x.CheckoutHeader.LastName).NotEmpty();
            RuleFor(x => x.CheckoutHeader.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.CheckoutHeader.Phone).NotEmpty();
        }
    }

    internal class Handler(
        ShoppingCartDbContext dbContext,
        ICouponsApi couponsApi,
        IEventBus eventBus
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var checkoutHeader = request.CheckoutHeader;

            // 1. Get current cart
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(u => u.UserId == checkoutHeader.UserId, cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            var cartDetails = await dbContext.CartDetails
                .Include(u => u.Product)
                .Where(u => u.CartHeaderId == cartHeader.Id)
                .ToListAsync(cancellationToken);

            // 2. Map cart details to checkout header
            checkoutHeader.CartDetails = cartDetails.Select(d => new CartDetailsDto
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
            });

            // 3. Verify coupon if present
            if (!string.IsNullOrEmpty(checkoutHeader.CouponCode))
            {
                var couponDto = await couponsApi.GetCouponAsync(checkoutHeader.CouponCode);
                if (couponDto != null && checkoutHeader.DiscountTotal != couponDto.DiscountAmount)
                {
                    throw new DataVerificationException("Coupon price has changed, please confirm");
                }
            }

            // 4. Send message to checkout queue (logic placeholder)
            // TODO: Inject IEventBus or similar and publish message

            var checkedOutEvent = new CartCheckedOutEvent
            {
                UserId = checkoutHeader.UserId,
                CartId = cartHeader.Id,
                FirstName = checkoutHeader.FirstName,
                LastName = checkoutHeader.LastName,
                Email = checkoutHeader.Email,
                Phone = checkoutHeader.Phone,
                CouponCode = checkoutHeader.CouponCode,
                CardNumber = checkoutHeader.CardNumber,
                CVV = checkoutHeader.CVV,
                DiscountTotal = checkoutHeader.DiscountTotal,
                ExpiryMonthYear = checkoutHeader.ExpiryMonthYear,
                OrderTotal = checkoutHeader.OrderTotal,
                PickupDate = checkoutHeader.PickupDate,
                CartTotalItems = checkoutHeader.CartTotalItems,
                CartDetails = checkoutHeader.CartDetails.Select(d => new CartCheckedOutEvent.CartDetailsDto
                {
                    Id = d.Id,
                    CartHeaderId = d.CartHeaderId,
                    ProductId = d.ProductId,
                    Count = d.Count,
                }).ToList()
            };

            await eventBus.PublishAsync(checkedOutEvent);

            // 5. Clear cart
            dbContext.CartDetails.RemoveRange(cartDetails);

            dbContext.CartHeaders.Remove(cartHeader);

            await dbContext.SaveChangesAsync(cancellationToken);

            return ResultModel<bool>.Create(true);
        }
    }
}
