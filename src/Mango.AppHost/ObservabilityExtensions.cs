namespace Mango.AppHost;

/// <summary>
/// Declares the Grafana + Prometheus + Loki telemetry stack that runs alongside
/// the Aspire dashboard, and wires the services up to it.
/// </summary>
/// <remarks>
/// Services push OTLP to an OpenTelemetry Collector, which fans logs out to Loki
/// and metrics out to Prometheus. Grafana reads from both. Traces are not part of
/// this stack and continue to go to the Aspire dashboard only.
/// </remarks>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Environment variable that <c>Mango.ServiceDefaults</c> reads to decide whether
    /// to mirror telemetry into the collector. Absent means "Aspire dashboard only".
    /// </summary>
    private const string OtlpEndpointVariable = "OBSERVABILITY_OTLP_ENDPOINT";

    private const string ConfigDirectory = "./init-configs/observability";

    /// <summary>
    /// Adds the stack unless <c>UseGrafanaStack</c> is configured to <c>false</c>.
    /// </summary>
    /// <returns>
    /// The collector's OTLP/gRPC endpoint, or <see langword="null"/> when the stack is
    /// switched off — in which case <see cref="WithGrafanaTelemetry{T}"/> is a no-op.
    /// </returns>
    public static EndpointReference? AddGrafanaObservability(this IDistributedApplicationBuilder builder)
    {
        // Declared in appsettings.json; overridable with the UseGrafanaStack environment
        // variable or --UseGrafanaStack false on the CLI. Absent means enabled.
        var setting = builder.Configuration["UseGrafanaStack"]?.Trim();
        var enabled = true;

        if (!string.IsNullOrEmpty(setting) && !bool.TryParse(setting, out enabled))
        {
            throw new InvalidOperationException(
                $"Invalid UseGrafanaStack '{setting}'. Allowed values are 'true' or 'false'.");
        }

        if (!enabled)
        {
            return null;
        }

        var loki = builder.AddContainer("loki", "grafana/loki", "3.7.6")
            .WithArgs("-config.file=/etc/loki/loki.yaml")
            .WithBindMount($"{ConfigDirectory}/loki.yaml", "/etc/loki/loki.yaml", isReadOnly: true)
            .WithVolume("mango-loki-data", "/loki")
            .WithHttpEndpoint(port: 3100, targetPort: 3100, name: "http")
            .WithLifetime(ContainerLifetime.Persistent);

        var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.13.2")
            .WithArgs(
                "--config.file=/etc/prometheus/prometheus.yml",
                "--storage.tsdb.path=/prometheus",
                "--storage.tsdb.retention.time=7d",
                // Lets the collector push metrics to /api/v1/otlp instead of
                // Prometheus having to scrape every service.
                "--web.enable-otlp-receiver",
                "--web.enable-lifecycle")
            .WithBindMount($"{ConfigDirectory}/prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true)
            .WithVolume("mango-prometheus-data", "/prometheus")
            .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "http")
            .WithLifetime(ContainerLifetime.Persistent);

        builder.AddContainer("grafana", "grafana/grafana", "13.1.3")
            .WithBindMount($"{ConfigDirectory}/grafana/provisioning", "/etc/grafana/provisioning", isReadOnly: true)
            .WithBindMount($"{ConfigDirectory}/grafana/dashboards", "/etc/grafana/dashboards", isReadOnly: true)
            .WithVolume("mango-grafana-data", "/var/lib/grafana")
            // Local-only convenience: no login screen, and therefore no credentials
            // checked into the repository.
            .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
            .WithEnvironment("GF_AUTH_BASIC_ENABLED", "false")
            .WithEnvironment("GF_ANALYTICS_REPORTING_ENABLED", "false")
            .WithEnvironment("GF_ANALYTICS_CHECK_FOR_UPDATES", "false")
            .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
            .WithExternalHttpEndpoints()
            .WaitFor(loki)
            .WaitFor(prometheus)
            .WithLifetime(ContainerLifetime.Persistent);

        // Hostnames in otel-collector.yaml are the Aspire resource names above;
        // Aspire puts every container on a shared network where those resolve.
        var collector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.158.0")
            .WithArgs("--config=/etc/otelcol/config.yaml")
            .WithBindMount($"{ConfigDirectory}/otel-collector.yaml", "/etc/otelcol/config.yaml", isReadOnly: true)
            .WithEndpoint(port: 4317, targetPort: 4317, scheme: "http", name: "otlp-grpc")
            .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
            .WithHttpEndpoint(port: 13133, targetPort: 13133, name: "health")
            .WaitFor(loki)
            .WaitFor(prometheus)
            .WithLifetime(ContainerLifetime.Persistent);

        return collector.GetEndpoint("otlp-grpc");
    }

    /// <summary>
    /// Points a resource at the collector. Does nothing when the stack is switched off,
    /// which leaves the resource exporting to the Aspire dashboard alone.
    /// </summary>
    public static IResourceBuilder<T> WithGrafanaTelemetry<T>(
        this IResourceBuilder<T> builder,
        EndpointReference? otlpEndpoint)
        where T : IResourceWithEnvironment
        => otlpEndpoint is null
            ? builder
            : builder.WithEnvironment(OtlpEndpointVariable, otlpEndpoint);
}
