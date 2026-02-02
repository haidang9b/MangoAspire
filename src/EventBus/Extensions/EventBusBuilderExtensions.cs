using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace EventBus.Extensions;

public static class EventBusBuilderExtensions
{
    public static IEventBusBuilder ConfigureJsonOptions(this IEventBusBuilder eventBusBuilder, Action<JsonSerializerOptions> configure)
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            configure(o.JsonSerializerOptions);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
        this IEventBusBuilder eventBusBuilder,
        string topicName,
        string subscriptionName
    ) where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var key = new SubscriptionInfo(topicName, subscriptionName);
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(new SubscriptionInfo(topicName, subscriptionName).ToString());

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.SubscriptionTypes[key] = typeof(T);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddConsumer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
        this IEventBusBuilder eventBusBuilder,
        string queueName
    ) where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        var key = new ConsumerInfo(queueName);
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(new ConsumerInfo(queueName).ToString());

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.ConsumerTypes[queueName] = typeof(T);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddQueue<T>(this IEventBusBuilder eventBusBuilder, string queueName) where T : IntegrationEvent
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.QueueTypes[queueName] = typeof(T);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddTopic<T>(
        this IEventBusBuilder eventBusBuilder,
        string topicName
    ) where T : IntegrationEvent
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.TopicTypes[topicName] = typeof(T);
        });

        return eventBusBuilder;
    }
}
