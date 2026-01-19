using EventBus.Abstractions;
using FluentValidation;
using Mango.Core.Domain;
using MediatR;
using Orders.API.Data;
using Orders.API.Entities;
using Orders.API.Extensions;
using Orders.API.Intergrations.Events;

namespace Orders.API.Features.Orders;

public class CreateOrder
{
    public class Command : ICommand<Guid>
    {
        public required CartCheckedOutEvent Event { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Event).NotNull();
            RuleFor(x => x.Event.UserId).NotEmpty();
            RuleFor(x => x.Event.Email).NotEmpty().EmailAddress();
        }
    }

    internal class Handler(
        OrdersDbContext dbContext,
        IEventBus eventBus
    ) : IRequestHandler<Command, ResultModel<Guid>>
    {
        public async Task<ResultModel<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var @event = request.Event;

            OrderHeader orderHeader = new()
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                OrderDetails = new List<OrderDetails>(),
                CardNumber = @event.CardNumber,
                CouponCode = @event.CouponCode,
                CVV = @event.CVV,
                DiscountTotal = @event.DiscountTotal,
                Email = @event.Email,
                ExpiryMonthYear = @event.ExpiryMonthYear,
                OrderTime = DateTime.Now,
                OrderTotal = @event.OrderTotal,
                PaymentStatus = false,
                Phone = @event.Phone,
                PickupDate = @event.PickupDate,
            };

            foreach (var detailList in @event.CartDetails)
            {
                OrderDetails orderDetails = new OrderDetails
                {
                    ProductId = detailList.Product.Id,
                    ProductName = detailList.Product.Name,
                    Price = detailList.Product.Price,
                    Count = detailList.Count,
                };
                orderHeader.CartTotalItems += detailList.Count;
                orderHeader.OrderDetails.Add(orderDetails);
            }

            dbContext.OrderHeaders.Add(orderHeader);
            await dbContext.SaveChangesAsync(cancellationToken);

            var paymentRequest = new CreatePaymentRequestCommand
            {
                Name = orderHeader.FullName,
                CardNumber = orderHeader.CardNumber,
                CVV = @event.CVV,
                ExpiryMonthYear = orderHeader.ExpiryMonthYear,
                OrderId = orderHeader.Id,
                OrderTotal = orderHeader.OrderTotal,
                Email = orderHeader.Email,
            };

            await eventBus.PublishAsync(paymentRequest);

            return ResultModel<Guid>.Create(orderHeader.Id);
        }
    }
}
