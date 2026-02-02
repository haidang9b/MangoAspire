using EventBus.Events;

namespace EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
}
