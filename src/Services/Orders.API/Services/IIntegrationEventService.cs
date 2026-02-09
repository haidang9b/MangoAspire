using EventBus.Events;

namespace Orders.API.Services;

public interface IIntegrationEventService
{
    Task PublishEventsThroughEventBusAsync(Guid transactionId);

    Task AddAndSaveEventAsync(IntegrationEvent evt);
}
