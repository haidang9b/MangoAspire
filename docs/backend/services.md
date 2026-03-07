# Backend Services Architecture

The back-end microservices of MangoAspire are fundamentally built on **.NET 10** utilizing a **Vertical Slice Architecture**.

## Vertical Slice Architecture
Instead of separating code technically by layers (e.g., Controllers, Services, Repositories), code is grouped by feature. 

Each feature (e.g., `CreateProduct`, `GetOrderById`) typically contains its own:
- Command/Query definition.
- Request Handler.
- Validation logic.
- DTOs.

This maximizes code cohesion and guarantees that changes to one feature won't inadvertently break another.

## MediatR and CQRS
We heavily use the **MediatR** library to implement the Command Query Responsibility Segregation (CQRS) pattern. HTTP endpoints do not contain business logic; they immediately dispatch a request to its corresponding handler.

```csharp
// Example structure using C# 14 Primary Constructors
public record CreateProductCommand(string Name, decimal Price) : IRequest<ResultModel<Guid>>;

internal class Handler(ProductDbContext dbContext) : IRequestHandler<CreateProductCommand, ResultModel<Guid>>
{
    public async Task<ResultModel<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
    }
}
```

## Resilience and Observability
All external HTTP communications and inter-service dependencies are decorated with Polly resilience pipelines (Retries, Circuit Breakers) managed through generic `.NET Aspire Service Defaults`.
Additionally, all logs, traces, and metrics are instrumented out-of-the-box with **OpenTelemetry**.
