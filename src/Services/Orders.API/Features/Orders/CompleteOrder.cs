using Microsoft.EntityFrameworkCore;

namespace Orders.API.Features.Orders;

public class CompleteOrder
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
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Completing order {OrderId}", request.OrderId);

            var order = await dbContext.OrderHeaders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order not found: {OrderId}", request.OrderId);
                return ResultModel<bool>.Create(false, true, $"Order {request.OrderId} not found");
            }

            order.PaymentStatus = true;
            order.Status = OrderStatus.Completed;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} completed successfully", request.OrderId);

            return ResultModel<bool>.Create(true);
        }
    }
}
