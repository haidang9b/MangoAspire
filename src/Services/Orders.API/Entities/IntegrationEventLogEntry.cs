using EventBus.Events;
using System.Text.Json;

namespace Orders.API.Entities;

public class IntegrationEventLogEntry
{
    public Guid EventId { get; private set; }

    public string EventTypeName { get; private set; } = string.Empty;

    public DateTime CreationTime { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public Guid TransactionId { get; private set; }

    public IntegrationEventLogEntry(IntegrationEvent integrationEvent, Guid transactionId)
    {
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), s_indentedOptions);
        EventTypeName = integrationEvent.GetType().AssemblyQualifiedName ?? string.Empty;
        CreationTime = DateTime.UtcNow;
        EventId = integrationEvent.Id;
        TransactionId = transactionId;
    }

    public IntegrationEventLogEntry() { }

    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };

    public object? DeserializeEvent(Type eventType)
    {
        return JsonSerializer.Deserialize(Content, eventType);
    }
}
