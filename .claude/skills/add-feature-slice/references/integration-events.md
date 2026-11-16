# Integration events

Moved from the former API-project-structure convention doc and corrected against `src/Services/Products.API` (in-house mediator in `src/Shared/Mediator`, not MediatR).

## Event-Driven Architecture

The solution uses an abstracted Event Bus (RabbitMQ / Azure Service Bus) for asynchronous service-to-service communication.

### 1. Publishing Events
Inject `IEventBus` into your mediator handlers to publish integration events.

```csharp
internal class CreateOrderHandler(IEventBus eventBus, MyDbContext db) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken ct)
    {
        // ... save order ...
        await eventBus.PublishAsync(new OrderCreatedEvent(order.Id));
        return Result.Success();
    }
}
```

### 2. Handling Events
Event subscriptions are configured in `Program.cs` and handlers are implemented in the `IntegrationEvents/Handlers` folder.

```csharp
// Program.cs
builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<OrderCreatedEvent, OrderCreatedHandler>("orders.events");
```

See [Event Bus Documentation](../../../../docs/EVENT_BUS.md) for detailed configuration and switching instructions.
