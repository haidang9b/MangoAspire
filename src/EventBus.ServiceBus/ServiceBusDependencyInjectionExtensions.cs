using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace EventBus.ServiceBus;

public static class ServiceBusDependencyInjectionExtensions
{
    private const string SectionName = "EventBus";

    extension(IHostApplicationBuilder builder)
    {
        public IEventBusBuilder AddServiceBusEventBus(string connectionName)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));
            builder.AddAzureServiceBusClient(connectionName);

            builder.Services.AddOpenTelemetry()
               .WithTracing(tracing =>
               {
                   tracing.AddSource(ServiceBusTelemetry.ActivitySourceName);
               });

            builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));

            builder.Services.AddSingleton<ServiceBusTelemetry>();
            builder.Services.AddSingleton<IEventBus, ServiceBusEventBus>();

            builder.Services.AddSingleton<IHostedService>(sp => (ServiceBusEventBus)sp.GetRequiredService<IEventBus>());

            return new ServiceBusEventBusBuilder(builder.Services);
        }
    }

    private class ServiceBusEventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }

    extension(IEventBusBuilder eventBusBuilder)
    {
        public IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
            string topicName,
            string subscriptionName
        ) where T : IntegrationEvent
            where TH : class, IIntegrationEventHandler<T>
        {
            var key = new SubscriptionInfo(topicName, subscriptionName);
            eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(new SubscriptionInfo(topicName, subscriptionName).ToString());

            eventBusBuilder.Services.Configure<ServiceBusInfo>(o =>
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

            eventBusBuilder.Services.Configure<ServiceBusInfo>(o =>
            {
                o.ConsumerTypes[queueName] = typeof(T);
            });

            return eventBusBuilder;
        }

        public IEventBusBuilder AddQueue<T>(string queueName) where T : IntegrationEvent
        {
            eventBusBuilder.Services.Configure<ServiceBusInfo>(o =>
            {
                o.QueueTypes[queueName] = typeof(T);
            });

            return eventBusBuilder;
        }

        public IEventBusBuilder AddTopic<T>(
            string topicName
        ) where T : IntegrationEvent
        {
            eventBusBuilder.Services.Configure<ServiceBusInfo>(o =>
            {
                o.TopicTypes[topicName] = typeof(T);
            });

            return eventBusBuilder;
        }
    }
}
