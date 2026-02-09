namespace Orders.API.Entities;

public class IntegrationEventLogEntry
{
    public Guid EventId { get; set; }

    public required string EventTypeName { get; set; }

    public DateTime CreationTime { get; set; }

    public required string Content { get; set; }

    public Guid TransactionId { get; set; }
}
