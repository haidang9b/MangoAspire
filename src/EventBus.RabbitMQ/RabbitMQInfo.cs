namespace EventBus.RabbitMQ;

public class RabbitMQInfo
{
    /// <summary>
    /// List of (ExchangeName, EventType) pairs for subscription bindings.
    /// Supports multiple event types per exchange.
    /// </summary>
    public List<(string ExchangeName, Type EventType)> EventTypes { get; } = [];
}

public record EventTypeInfo(string Name, string? Type = "direct");
