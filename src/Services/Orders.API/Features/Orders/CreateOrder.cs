namespace Orders.API.Features.Orders;

public class CreateOrder
{
    public class Command : IIdentifiedCommand<Guid>
    {
        public required Guid CorrelationId { get; init; }

        public required CartCheckedOutEvent Event { get; init; }

        public Guid Id { get; set; }

        public Guid CreateDefaultResponse()
        {
            return Guid.NewGuid();
        }
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

            var orderHeader = new OrderHeader
            {
                Id = Guid.NewGuid(),
                CartTotalItems = @event.CartTotalItems,
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
                Status = Enums.OrderStatus.Processing
            };

            foreach (var detail in @event.CartDetails)
            {
                orderHeader.OrderDetails.Add(new OrderDetails
                {
                    ProductId = detail.ProductId,
                    Count = detail.Count,
                });
            }

            dbContext.OrderHeaders.Add(orderHeader);
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventBus.PublishAsync(new OrderCreatedEvent(request.CorrelationId, orderHeader.Id));

            return ResultModel<Guid>.Create(orderHeader.Id);
        }
    }
}
