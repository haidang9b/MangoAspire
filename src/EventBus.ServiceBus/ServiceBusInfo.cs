namespace EventBus.ServiceBus;

public class ServiceBusInfo
{
    public Dictionary<SubscriptionInfo, Type> SubscriptionTypes { get; } = [];

    public Dictionary<string, Type> QueueTypes { get; } = [];

    public Dictionary<string, Type> ConsumerTypes { get; } = [];

    public Dictionary<string, Type> TopicTypes { get; } = [];
}
public record SubscriptionInfo(string TopicName, string SubscriptionName)
{
    public override string ToString() => $"topic:{TopicName}.subscription:{SubscriptionName}";
}

public record ConsumerInfo(string QueueName)
{
    public override string ToString() => $"queue:{QueueName}";
}
