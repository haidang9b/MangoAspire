using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace EventBus.Extensions;

public static class EventBusBuilderExtensions
{
    extension(IEventBusBuilder eventBusBuilder)
    {
        public IEventBusBuilder ConfigureJsonOptions(Action<JsonSerializerOptions> configure)
        {
            eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
            {
                configure(o.JsonSerializerOptions);
            });

            return eventBusBuilder;
        }

        public IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
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

        public IEventBusBuilder AddConsumer<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
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

        public IEventBusBuilder AddQueue<T>(string queueName) where T : IntegrationEvent
        {
            eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
            {
                o.QueueTypes[queueName] = typeof(T);
            });

            return eventBusBuilder;
        }

        public IEventBusBuilder AddTopic<T>(
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
}
