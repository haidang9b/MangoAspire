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
            var checkOutHeader = request.CheckoutHeader;

            // 1. Get current cart
            var cartHeader = await dbContext.CartHeaders
                .FirstOrDefaultAsync(u => u.UserId == checkOutHeader.UserId, cancellationToken)
                ?? throw new DataVerificationException("Cart not found");

            var cartDetails = await dbContext.CartDetails
                .Include(u => u.Product)
                .Where(u => u.CartHeaderId == cartHeader.Id)
                .ToListAsync(cancellationToken);

            // 2. Map cart details to checkout header
            checkOutHeader.CartDetails = cartDetails.Select(d => new CartDetailsDto
            {
                Id = d.Id,
                CartHeaderId = d.CartHeaderId,
                ProductId = d.ProductId,
                Count = d.Count,

            });

            // 3. Verify coupon if present
            if (!string.IsNullOrEmpty(checkOutHeader.CouponCode))
            {
                var couponDto = await couponsApi.GetCouponAsync(checkOutHeader.CouponCode);
                if (couponDto != null && checkOutHeader.DiscountTotal != couponDto.DiscountAmount)
                {
                    throw new DataVerificationException("Coupon price has changed, please confirm");
                }
            }

            // 4. Send message to checkout queue (logic placeholder)
            // TODO: Inject IEventBus or similar and publish message

            var checkedOutEvent = new CartCheckedOutEvent
            {
                UserId = checkOutHeader.UserId,
                CartId = cartHeader.Id,
                FirstName = checkOutHeader.FirstName,
                LastName = checkOutHeader.LastName,
                Email = checkOutHeader.Email,
                Phone = checkOutHeader.Phone,
                CouponCode = checkOutHeader.CouponCode ?? string.Empty,
                CardNumber = checkOutHeader.CardNumber,
                CVV = checkOutHeader.CVV,
                DiscountTotal = checkOutHeader.DiscountTotal,
                ExpiryMonthYear = checkOutHeader.ExpiryMonthYear,
                OrderTotal = checkOutHeader.OrderTotal,
                PickupDate = checkOutHeader.PickupDate,
                CartTotalItems = checkOutHeader.CartTotalItems,
                CartDetails = checkOutHeader.CartDetails.Select(d => new CartCheckedOutEvent.CartDetailsDto
                {
                    Id = d.Id,
                    CartHeaderId = d.CartHeaderId,
                    Count = d.Count,
                    ProductId = d.ProductId,

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
