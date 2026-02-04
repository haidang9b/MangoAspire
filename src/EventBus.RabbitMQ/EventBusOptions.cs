namespace EventBus.RabbitMQ;

public class EventBusOptions
{
    /// <summary>
    /// Each subscription is a queue present for service listening to events
    /// Each message in subscription is an event, consumer will process base on routing key
    /// </summary>
    public required string SubscriptionClientName { get; set; }

    /// <summary>
    /// Domain name name is used to scope exchanges base on domain
    /// For example: "OrderService" => "orders.events
    /// </summary>
    public required string DomainName { get; set; }

    public int RetryCount { get; set; } = 10;
}
