# Mediator

A lightweight, custom Mediator library for the MangoAspire project. It is a drop-in alternative to MediatR, built on top of the standard `Microsoft.Extensions.DependencyInjection` abstractions.

## Why a Custom Mediator?

- **No third-party dependency**: Full control over the pipeline without relying on an external package.
- **Performance**: Uses a `ConcurrentDictionary` cache to avoid reflection on every request dispatch (only incurred on first call per request type).
- **Compatibility**: Interfaces and conventions are intentionally compatible with MediatR — `IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>` all behave the same way.

---

## Project Structure

```
Mediator/
├── Abstractions/
│   ├── IRequest.cs         — IRequest<T>, IRequestHandler<,>, IPipelineBehavior<,>
│   ├── INotification.cs    — INotification, INotificationHandler<T>
│   └── IMediator.cs        — ISender, IPublisher, IMediator
├── Extensions/
│   └── ServiceCollectionExtensions.cs — AddMediator(params Assembly[])
├── Mediator.cs             — Concrete implementation + Wrappers + Caching
└── Mediator.csproj
```

---

## Core Abstractions

### `IRequest<TResponse>`
A marker interface for any request/command/query that returns a response.

```csharp
// Mediator.Abstractions
public interface IRequest<out TResponse> { }
```

### `IRequestHandler<TRequest, TResponse>`
The handler for a specific request type. One handler per request type.

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

### `IPipelineBehavior<TRequest, TResponse>`
A middleware-style wrapper around handler execution. Register multiple behaviors to build a pipeline.

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

> **Note:** `IPipelineBehavior<,>` intentionally has **no constraint on `TRequest`** (unlike MediatR). This allows it to work correctly with domain interfaces like `ICommand<T>` where the actual `TResponse` is `ResultModel<T>`.

### `ISender` / `IPublisher` / `IMediator`

```csharp
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public interface IPublisher
{
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

public interface IMediator : ISender, IPublisher { }
```

### `INotification` / `INotificationHandler<T>`

Notifications are fan-out messages. **Multiple handlers** can exist for the same notification, and they are all called sequentially.

```csharp
public interface INotification { }

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

---

## Project Integration (`Mango.Core`)

Commands and Queries in `Mango.Core` extend `IRequest<T>` transparently:

```csharp
// ICommand<T> implements IRequest<ResultModel<T>>
public interface ICommand<T> : IRequest<ResultModel<T>>, ITransactionRequest
    where T : notnull { }

// IQuery<T> implements IRequest<ResultModel<T>>
public interface IQuery<T> : IRequest<ResultModel<T>>
    where T : notnull { }
```

This means existing command/query handlers work without any changes.

---

## How to Use

### 1. Register the Mediator

In your `IServiceCollectionExtensions.cs` (or `Program.cs`):

```csharp
using Mediator.Abstractions;
using Mediator.Extensions;
using Mango.Core.Behaviors;
using Mango.Infrastructure.Behaviors;

// Register the mediator and auto-scan for IRequestHandler implementations
services.AddMediator(typeof(Program).Assembly);

// Register pipeline behaviors (order matters — first registered = outermost wrapper)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentifiedBehavior<,>)); // Idempotency
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));    // Logging
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // Validation
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TxBehavior<,>));         // Transaction
```

### 2. Define a Command and Handler

```csharp
// Command
public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;

// Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ResultModel<Guid>>
{
    public async Task<ResultModel<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        // ... your business logic
        return ResultModel<Guid>.Create(newId);
    }
}
```

No registration needed — `AddMediator(typeof(Program).Assembly)` auto-discovers all handlers.

### 3. Send a Request

Inject `ISender` or `IMediator` into your endpoint or service:

```csharp
app.MapPost("/products", async (CreateProductCommand cmd, ISender sender) =>
{
    var result = await sender.Send(cmd);
    return result.IsError ? Results.BadRequest(result.ErrorMessage) : Results.Ok(result.Data);
});
```

### 4. Add a Custom Pipeline Behavior

```csharp
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Pre-handling logic
        var response = await next();
        // Post-handling logic
        return response;
    }
}

// Register it
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>));
```

---

## Pipeline Execution Order

Behaviors are executed in the order they are registered. The final call is always the handler:

```
Request → IdentifiedBehavior → LoggingBehavior → ValidationBehavior → TxBehavior → Handler
                                                                                  ↑
                                                                            (innermost)
```

---

## Performance: Caching

The `Mediator` implementation uses a `ConcurrentDictionary<Type, RequestHandlerWrapper>` to cache handler execution for each request type. Reflection is only used **once per request type** during the first call to `Send`. Subsequent calls dispatch directly through typed delegates — no reflection in the hot path.

```
1st call:  Type lookup → Reflection (create wrapper) → Cache → Execute
Nth call:  Type lookup → Cache hit → Execute  ✅ No reflection
```
