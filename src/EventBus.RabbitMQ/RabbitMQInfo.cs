namespace EventBus.RabbitMQ;

public class RabbitMQInfo
{
    public Dictionary<string, Type> EventTypes { get; } = [];
}

public record EventTypeInfo(string Name, string? Type = "direct");
