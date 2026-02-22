namespace Mediator.Abstractions;

/// <summary>Sends a request through the mediator pipeline.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>Main mediator interface combining send capabilities.</summary>
public interface IMediator : ISender { }
