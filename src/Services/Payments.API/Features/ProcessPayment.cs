using EventBus.Abstractions;
using FluentValidation;
using Mango.Core.Domain;
using Mango.Events.Orders;
using MediatR;
using Microsoft.Extensions.Options;
using Payments.API.Configurations;

namespace Payments.API.Features;

public class ProcessPayment
{
    public class Command : ICommand<bool>
    {
        public required Guid CorrelationId { get; init; }
        public required Guid OrderId { get; init; }
        public required decimal OrderTotal { get; init; }
        public required string CardNumber { get; init; }
        public required string CVV { get; init; }
        public required string ExpiryMonthYear { get; init; }
        public required string Email { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CorrelationId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderTotal).GreaterThan(0);
            RuleFor(x => x.CardNumber).NotEmpty();
            RuleFor(x => x.CVV).NotEmpty();
        }
    }

    internal class Handler(
        IEventBus eventBus,
        IOptionsMonitor<PaymentOptions> options,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ResultModel<bool>>
    {
        public async Task<ResultModel<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}",
                request.OrderId, request.OrderTotal);

            // Simulate payment processing delay
            await Task.Delay(100, cancellationToken);

            if (options.CurrentValue.PaymentSucceeded)
            {
                logger.LogInformation("Payment succeeded for order {OrderId}", request.OrderId);
                await eventBus.PublishAsync(new PaymentSucceededEvent(request.CorrelationId));
                return ResultModel<bool>.Create(true);
            }
            else
            {
                logger.LogWarning("Payment failed for order {OrderId}", request.OrderId);
                await eventBus.PublishAsync(new PaymentFailedEvent(request.CorrelationId, "Payment declined"));
                return ResultModel<bool>.Create(false, true, "Payment declined");
            }
        }
    }
}
