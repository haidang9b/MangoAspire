using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace EventBus.ServiceBus;

public class ServiceBusTelemetry
{
    public static string ActivitySourceName = "EventBusServiceBus";

    public ActivitySource ActivitySource { get; } = new(ActivitySourceName);
    public TextMapPropagator Propagator { get; } = Propagators.DefaultTextMapPropagator;
}
