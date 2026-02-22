namespace Mediator.Abstractions;

/// <summary>Sends a request through the mediator pipeline.</summary>
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>Publishes a notification to all registered handlers.</summary>
public interface IPublisher
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

/// <summary>Main mediator interface combining send and publish capabilities.</summary>
public interface IMediator : ISender, IPublisher { }
