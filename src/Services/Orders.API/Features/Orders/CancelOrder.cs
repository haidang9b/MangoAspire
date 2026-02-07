using EventBus.Abstractions;
using FluentValidation;
using Mango.Core.Domain;
using Mango.Events.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.API.Data;
using Orders.API.Enums;

namespace Orders.API.Features.Orders;

public class CancelOrder
{
    public class Command : ICommand<bool>
    {
        public required Guid CorrelationId { get; init; }
        public required Guid OrderId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }

    internal class Handler(
        OrdersDbContext dbContext,
        IEventBus eventBus
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var order = await dbContext.OrderHeaders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                return ResultModel<bool>.Create(false, true, $"Order {request.OrderId} not found");
            }

            order.PaymentStatus = false;
            order.Status = OrderStatus.Cancelled;
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventBus.PublishAsync(new OrderCancelledEvent(request.CorrelationId, request.OrderId));

            return ResultModel<bool>.Create(true);
        }
    }
}
