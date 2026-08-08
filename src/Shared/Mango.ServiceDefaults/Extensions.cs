using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Mango.ServiceDefaults;
// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>Aspire injects this and points it at the Aspire dashboard.</summary>
    private const string DashboardEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// The AppHost injects this when the Grafana stack is running; it points at the
    /// OpenTelemetry Collector in front of Loki and Prometheus.
    /// </summary>
    private const string CollectorEndpointVariable = "OBSERVABILITY_OTLP_ENDPOINT";

    private const string CollectorExporterName = "grafana-stack";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useDashboardExporter = !string.IsNullOrWhiteSpace(builder.Configuration[DashboardEndpointVariable]);
        var useCollectorExporter = Uri.TryCreate(
            builder.Configuration[CollectorEndpointVariable], UriKind.Absolute, out var collectorEndpoint);

        if (!useCollectorExporter)
        {
            if (useDashboardExporter)
            {
                // UseOtlpExporter reads the OTEL_EXPORTER_OTLP_* variables Aspire injects.
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        // UseOtlpExporter claims exporter registration exclusively — combining it with a
        // signal-specific AddOtlpExporter throws NotSupportedException — so exporting to a
        // second backend means registering every exporter explicitly.
        if (useDashboardExporter)
        {
            builder.Services.AddOpenTelemetry()
                .WithLogging(logging => logging.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter())
                .WithTracing(tracing => tracing.AddOtlpExporter());
        }

        // Logs and metrics are mirrored to the collector. Traces are not: the collector
        // has no trace pipeline until a trace store is added alongside Loki and Prometheus.
        builder.Services.AddOpenTelemetry()
            .WithLogging(logging => logging.AddOtlpExporter(
                CollectorExporterName,
                options => ConfigureCollectorExporter(options, collectorEndpoint!)))
            .WithMetrics(metrics => metrics.AddOtlpExporter(
                CollectorExporterName,
                (options, _) => ConfigureCollectorExporter(options, collectorEndpoint!)));

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;

        // Named options still bind the OTEL_EXPORTER_OTLP_* variables first, so every
        // field the collector cares about is overwritten here rather than left inherited.
        static void ConfigureCollectorExporter(OtlpExporterOptions options, Uri endpoint)
        {
            options.Endpoint = endpoint;
            options.Protocol = OtlpExportProtocol.Grpc;
            // Drops the dashboard's API key: it is a secret for a different backend.
            options.Headers = string.Empty;
        }
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
