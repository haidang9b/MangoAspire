using EventBus.Abstractions;

namespace EventBus.RabbitMQ;

/// <summary>
/// Maps a CLR event type to the routing key it travels under: the
/// <see cref="EventNameAttribute"/> value when present, otherwise the type name.
/// </summary>
/// <remarks>
/// CDC events rely on the attribute, because their routing key is the Debezium topic name
/// (<c>mango.public.products</c>) rather than anything derivable from the C# type.
/// </remarks>
internal static class EventRoutingKey
{
    public static string For(Type eventType)
        => eventType.GetCustomAttributes(typeof(EventNameAttribute), true).FirstOrDefault() is EventNameAttribute attribute
            ? attribute.Name
            : eventType.Name;

    public static string For(object @event) => For(@event.GetType());
}
