namespace Mediator.Abstractions;

/// <summary>Marker interface for a request that returns a response.</summary>
public interface IRequest<out TResponse> { }

/// <summary>Delegate for the next handler in the pipeline.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Handles a request of type <typeparamref name="TRequest"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Pipeline behavior that wraps handler execution. No constraint on TRequest so it works with derived interfaces like ICommand.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
