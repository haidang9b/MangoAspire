namespace Mediator.Abstractions;

/// <summary>Marker interface for a notification that can be published to multiple handlers.</summary>
public interface INotification { }

/// <summary>Handles a notification of type <typeparamref name="TNotification"/>.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
