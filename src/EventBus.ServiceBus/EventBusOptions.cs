namespace EventBus.ServiceBus;

public class EventBusOptions
{
    public required string SubscriptionClientName { get; set; }
    public int RetryCount { get; set; } = 10;
}
