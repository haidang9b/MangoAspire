using EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
