using EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
}
