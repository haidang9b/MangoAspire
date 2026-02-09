using EventBus.Events;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Orders.API.Services;

public class IntegrationEventService : IIntegrationEventService
{
    private readonly IEventBus _eventBus;

    private readonly ILogger<IntegrationEventService> _logger;

    private readonly OrdersDbContext _ordersDbContext;

    private readonly ConcurrentDictionary<string, Type> _types = new();

    public IntegrationEventService(
        IEventBus eventBus,
        ILogger<IntegrationEventService> logger,
        OrdersDbContext ordersDbContext
    )
    {
        _eventBus = eventBus;
        _logger = logger;
        _ordersDbContext = ordersDbContext;
    }

    public async Task AddAndSaveEventAsync(IntegrationEvent evt)
    {
        var transactionId = _ordersDbContext.Database.CurrentTransaction?.TransactionId ?? Guid.Empty;
        var eventLog = new IntegrationEventLogEntry(evt, transactionId);
        await _ordersDbContext.IntegrationEventLogEntry.AddAsync(eventLog);

        await _ordersDbContext.SaveChangesAsync();
    }

    public async Task PublishEventsThroughEventBusAsync(Guid transactionId)
    {
        var pendingEvents = await _ordersDbContext.IntegrationEventLogEntry
            .Where(e => e.TransactionId == transactionId)
            .AsNoTracking()
            .ToListAsync();

        foreach (var @event in pendingEvents)
        {

            var eventType = _types.GetOrAdd(@event.EventTypeName, type => Type.GetType(type)!);
            if (eventType == null)
            {
                _logger.LogError("Event type {EventTypeName} not found", @event.EventTypeName);
                continue;
            }
            var integrationEvent = @event.DeserializeEvent(eventType);

            if (integrationEvent == null)
            {
                _logger.LogError("Event type {EventTypeName} could not be deserialized", @event.EventTypeName);
                continue;
            }
            await _eventBus.PublishAsync(integrationEvent);
        }
    }
}
