using Microsoft.EntityFrameworkCore;
using Orders.API.Services;

namespace Orders.API.Features.Orders;

public class CancelOrder
{
    public class Command : IIdentifiedCommand<bool>
    {
        public Guid Id { get; set; }

        public required Guid CorrelationId { get; init; }

        public required Guid OrderId { get; init; }

        public string? CancelReason { get; init; }

        public bool CreateDefaultResponse()
        {
            return false;
        }
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
        IIntegrationEventService integrationEventService
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            var order = await dbContext.OrderHeaders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                return ResultModel<bool>.Create(false, true, $"Order {request.OrderId} not found");
            }

            order.PaymentStatus = false;
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = request.CancelReason;
            await dbContext.SaveChangesAsync(cancellationToken);

            await integrationEventService.AddAndSaveEventAsync(new OrderCancelledEvent(request.CorrelationId, request.OrderId));

            return ResultModel<bool>.Create(true);
        }
    }
}
