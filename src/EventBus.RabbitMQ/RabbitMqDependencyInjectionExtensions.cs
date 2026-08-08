using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace EventBus.RabbitMQ;

public static class RabbitMQDependencyInjectionExtensions
{
    private const string SectionName = "EventBus";
    private const string StreamSectionName = "Stream";

    public static IEventBusBuilder AddRabbitMQEventBus(this IHostApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Add RabbitMQ Client (Aspire)
        builder.AddRabbitMQClient(connectionName);

        builder.Services.AddOpenTelemetry()
           .WithTracing(tracing =>
           {
               tracing.AddSource(RabbitMQTelemetry.ActivitySourceName);
           });

        builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));
        builder.Services.Configure<StreamConsumerOptions>(builder.Configuration.GetSection($"{SectionName}:{StreamSectionName}"));

        builder.Services.AddSingleton<RabbitMQTelemetry>();
        builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();

        builder.Services.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

        // Starts no channels unless AddStreamSubscription registered something, so services
        // that only use classic queues are unaffected.
        builder.Services.AddSingleton<RabbitMqStreamConsumer>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RabbitMqStreamConsumer>());

        return new RabbitMQEventBusBuilder(builder.Services);
    }

    private class RabbitMQEventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }

    public static IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
        this IEventBusBuilder eventBusBuilder,
        string fromExchangeName
    ) where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        // Use keyed services to register multiple handlers for the same event type
        // the consumer can use IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(T)) to get all
        // handlers for the event type.
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));

        eventBusBuilder.Services.Configure<RabbitMQInfo>(o =>
        {
            // Keep track of all registered event types and their exchange mapping.
            // This list is used to subscribe to events from the underlying message broker.
            o.EventTypes.Add((fromExchangeName, typeof(T)));
        });

        return eventBusBuilder;
    }

    /// <summary>
    /// Subscribes to <typeparamref name="T"/> from a replayable stream log rather than a classic queue.
    /// </summary>
    /// <param name="fromStreamName">
    /// The stream queue to read — declared in the broker's definitions.json at boot, not here,
    /// so the log exists before any publisher or consumer does.
    /// </param>
    /// <remarks>
    /// Use this for change-data-capture, where a consumer must be able to rebuild its
    /// read-model from history: the service reads the log from its own stored offset, and a
    /// service with no stored offset replays everything retained. Keep
    /// <see cref="AddSubscription{T, TH}"/> for domain events, which want dead-lettering and
    /// redelivery rather than replay.
    /// <para>
    /// Requires an <see cref="ICdcOffsetStore"/> registration to persist the position.
    /// </para>
    /// </remarks>
    public static IEventBusBuilder AddStreamSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(
        this IEventBusBuilder eventBusBuilder,
        string fromStreamName
    ) where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));

        eventBusBuilder.Services.Configure<StreamSubscriptionInfo>(o =>
        {
            o.EventTypes.Add((fromStreamName, typeof(T)));
        });

        return eventBusBuilder;
    }
}
