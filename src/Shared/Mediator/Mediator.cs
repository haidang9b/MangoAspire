using Mediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Mediator;

/// <summary>
/// Default mediator implementation that resolves handlers from DI
/// and executes them through the registered pipeline behaviors.
/// Optimized with a caching layer to avoid reflection on every request.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, object> _handlerWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> _notificationWrappers = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        var wrapper = (RequestHandlerWrapper<TResponse>)_handlerWrappers.GetOrAdd(requestType, static (type) =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse));
            return Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for {type}");
        });

        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var notificationType = notification.GetType();

        var wrapper = (NotificationHandlerWrapper)_notificationWrappers.GetOrAdd(notificationType, static (type) =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(type);
            return Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create notification wrapper for {type}");
        });

        return wrapper.Handle(notification, _serviceProvider, cancellationToken);
    }
}

// ─── Request Handler Wrapper ─────────────────────────────────────────────────

internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse> where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Resolve handler: IRequestHandler<TRequest, TResponse>
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        // Build the innermost call — the actual handler
        RequestHandlerDelegate<TResponse> handlerCall = () => handler.HandleAsync(typedRequest, cancellationToken);

        // Resolve and wrap all IPipelineBehavior<TRequest, TResponse> in reverse order
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

        // Wrap behaviors from last to first so the first registered runs first
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var currentNext = handlerCall;
            handlerCall = () => behavior.HandleAsync(typedRequest, currentNext, cancellationToken);
        }

        return handlerCall();
    }
}

// ─── Notification Handler Wrapper ─────────────────────────────────────────────

internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;

        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(typedNotification, cancellationToken);
        }
    }
}

