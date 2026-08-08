using Mango.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Grafana + Prometheus + Loki, fronted by an OpenTelemetry Collector. Returns null
// (and starts nothing) when "UseGrafanaStack" is false, leaving the Aspire dashboard
// as the only telemetry sink.
var observability = builder.AddGrafanaObservability();

// Resolved from the "Parameters" section of appsettings.json, or overridden
// with user-secrets / environment variables (Parameters__postgres-password).
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", secret: true);

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    // pgvector/pgvector ships stock PostgreSQL plus the "vector" extension, which
    // chatagentdb needs for its embedding index. The tag must track the same major
    // version Aspire would otherwise start (18): PostgreSQL cannot read a data
    // directory written by a newer major, so an existing volume would fail to mount.
    .WithImage("pgvector/pgvector", "pg18")
    .WithLifetime(ContainerLifetime.Persistent)
    // Explicit volume rather than WithDataVolume(). Aspire derives the mount target from
    // the image tag, and "pg18" is not a parseable version, so it would fall back to the
    // pre-18 target of /var/lib/postgresql/data. PostgreSQL 18 images store data in
    // major-version-specific subdirectories under /var/lib/postgresql (PGDATA is
    // /var/lib/postgresql/18/docker), so the mount has to sit one level up. Mounting it
    // here also stops Docker creating a throwaway anonymous volume for the image's
    // declared VOLUME.
    .WithVolume("mango-postgres-data", "/var/lib/postgresql")
    .WithBindMount("./init-scripts/productdb", "/docker-entrypoint-initdb.d")
    // Debezium CDC uses the pgoutput plugin, which needs logical decoding.
    // wal_level cannot be set from SQL and postgresql.conf lives inside the
    // data volume, so pass it as a server argument.
    .WithArgs("-c", "wal_level=logical");

var productdb = postgres.AddDatabase("productdb");
var orderdb = postgres.AddDatabase("orderdb");
var coupondb = postgres.AddDatabase("coupondb");
var identitydb = postgres.AddDatabase("identitydb");
var openidentitydb = postgres.AddDatabase("openidentitydb");
var shoppingcartdb = postgres.AddDatabase("shoppingcartdb");
var sagaorchestratorsdb = postgres.AddDatabase("sagaorchestratorsdb");
var chatagentdb = postgres.AddDatabase("chatagentdb");

var rabbitMq = builder.AddRabbitMQ("eventbus", password: rabbitMqPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    // Stream retention is only meaningful if the log survives container recreation.
    .WithDataVolume("mango-rabbitmq-data")
    .WithManagementPlugin(port: 15672);

// -----------------------------------------------------------------
//  Identity provider feature switch
//  Allowed values: "Duende" (Identity.API) | "OpenIddict" (OpenIdentity.App)
//  Set via appsettings.json ("IdentityType"), the IdentityType /
//  IDENTITY_TYPE environment variables, or --IdentityType on the CLI.
// -----------------------------------------------------------------
var identityType = (builder.Configuration["IdentityType"]
        ?? builder.Configuration["IDENTITY_TYPE"]
        ?? "Duende")
    .Trim();
var useOpenIddict = identityType.Equals("OpenIddict", StringComparison.OrdinalIgnoreCase);

if (!useOpenIddict && !identityType.Equals("Duende", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"Invalid IdentityType '{identityType}'. Allowed values are 'Duende' or 'OpenIddict'.");
}

//var serviceBus = builder.AddAzureServiceBus("mango")
//    .RunAsEmulator();

//var checkedOutEventTopic = serviceBus
//    .AddServiceBusTopic("checked-out-events")
//    .AddServiceBusSubscription("checked-out-events-ordersapi");

//var createPaymentRequestQueue = serviceBus
//    .AddServiceBusQueue("create-payment-command");

//var orderPaymentFailedEventTopic = serviceBus
//    .AddServiceBusTopic("order-payment-failed-events")
//    .AddServiceBusSubscription("order-payment-failed-events-paymentsapi");

//var orderPaymentSucceededEventTopic = serviceBus
//    .AddServiceBusTopic("order-payment-succeeded-events")
//    .AddServiceBusSubscription("order-payment-succeeded-events-ordersapi");

// Only the selected identity provider is registered/started; every
// consumer resolves the authority through identityRef below.
var identityRef = useOpenIddict
    ? builder.AddProject<Projects.OpenIdentity_App>("openidentity-app")
        .WaitFor(openidentitydb)
        .WithReference(openidentitydb)
    : builder.AddProject<Projects.Identity_API>("identity-app")
        .WaitFor(identitydb)
        .WithReference(identitydb);

// Get identity endpoint for services that need JWT validation
var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";
var identityEndpoint = identityRef.GetEndpoint(launchProfileName);

var products = builder.AddProject<Projects.Products_API>("products-api")
    .WaitFor(productdb).WithReference(productdb)
    .WaitFor(rabbitMq).WithReference(rabbitMq);

var coupon = builder.AddProject<Projects.Coupons_API>("coupons-api")
    .WaitFor(coupondb)
    .WithReference(coupondb);

var shoppingcart = builder.AddProject<Projects.ShoppingCart_API>("shoppingcart-api")
    .WaitFor(rabbitMq).WithReference(rabbitMq)
    .WaitFor(shoppingcartdb)
    //.WaitFor(serviceBus)
    .WithReference(shoppingcartdb)
    //.WithReference(serviceBus)
    .WithReference(identityRef)
    .WithReference(coupon)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint);

var orders = builder.AddProject<Projects.Orders_API>("orders-api")
    .WaitFor(orderdb)
    .WaitFor(rabbitMq)
    //.WaitFor(serviceBus)
    .WithReference(orderdb)
    .WithReference(rabbitMq)
    .WithReference(identityRef)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint);
//.WithReference(serviceBus);


var payments = builder.AddProject<Projects.Payments_API>("payments-api")
    .WaitFor(rabbitMq)
    .WithReference(rabbitMq);
//.WaitFor(serviceBus)
//.WithReference(serviceBus);


// Products and categories are replicated into chatagentdb over Debezium CDC rather
// than fetched from products-api per chat turn, so the only reference needed here is
// the event bus. Carts and coupons stay direct because those are transactional writes.
var agentApp = builder.AddProject<Projects.ChatAgent_App>("chatagent-app")
    .WithReference(chatagentdb).WaitFor(chatagentdb)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(identityRef).WaitFor(identityRef)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint)
    .WithReference(coupon).WaitFor(coupon)
    .WithReference(shoppingcart).WaitFor(shoppingcart);

// Imports the CDC topology — the mango.cdc.stream log, mango-cdc-exchange, and the bindings
// between them — over the RabbitMQ management API once the broker is up.
//
// It has to be the HTTP API rather than the broker's own `load_definitions` setting: a node
// configured to load definitions at boot logs "Will not seed default virtual host and user:
// have definitions to load" and skips creating the default user entirely, so
// RABBITMQ_DEFAULT_USER/_PASS are ignored and every service is rejected with
// "PLAIN login refused". Definitions would then have to carry the users themselves, which is
// impossible here because the password is a generated secret parameter.
//
// Declaring the topology from infrastructure rather than from a consumer is the whole point:
// the log exists before Debezium publishes and regardless of which services are running, so a
// service introduced months later still replays the full history.
var cdcTopology = builder.AddContainer("cdc-topology", "curlimages/curl", "8.11.1")
    .WithBindMount("./init-configs/rabbitmq/definitions.json", "/definitions.json")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEnvironment("RABBITMQ_HOST", rabbitMq.Resource.Name)
    .WithEnvironment("RABBITMQ_PASSWORD", rabbitMqPassword)
    .WithEntrypoint("/bin/sh")
    // ReplaceLineEndings is not cosmetic: this file is stored with CRLF, and a raw string
    // literal keeps the source's line endings verbatim. Passing those to sh makes it read
    // "set -e\r" and fail with `illegal option -`.
    .WithArgs("-c", """
        set -e
        until curl -sf -u "guest:$RABBITMQ_PASSWORD" "http://$RABBITMQ_HOST:15672/api/overview" > /dev/null 2>&1; do
          echo "Waiting for the RabbitMQ management API..."
          sleep 2
        done
        curl -sS --fail-with-body -u "guest:$RABBITMQ_PASSWORD" \
          -H "content-type: application/json" \
          -X POST --data-binary @/definitions.json \
          "http://$RABBITMQ_HOST:15672/api/definitions"
        echo "CDC topology imported: mango.cdc.stream is ready."
        """.ReplaceLineEndings("\n"));

// Debezium waits for the topology, not for its consumers. mango-cdc-exchange is a direct
// exchange and RabbitMQ silently discards messages matching no binding, so the stream must
// exist before the initial snapshot — but nothing here depends on ShoppingCart or ChatAgent
// being up, which is what lets a new consumer be added later without losing history.
var debezium = builder.AddContainer("debezium", "debezium/server", "2.7.3.Final")
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "api")
    .WithBindMount("./init-configs/products/application.properties", "/debezium/conf/application.properties")
    .WithVolume("debezium-data", "/debezium/data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithReference(productdb).WaitFor(productdb)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WaitForCompletion(cdcTopology)
    // PostgreSQL connection
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_HOSTNAME", postgres.Resource.Name)
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_PORT", postgres.Resource.Port)
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_USER", "postgres")
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_PASSWORD", postgresPassword)
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_DBNAME", "productdb")
    // RabbitMQ connection
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_HOST", rabbitMq.Resource.Name)
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_PORT", "5672")
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_USERNAME", "guest")
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_PASSWORD", rabbitMqPassword);


var webApp = builder.AddProject<Projects.Mango_Web>("mango-web")
    .WithReference(identityRef)
    .WithReference(products)
    .WithReference(shoppingcart)
    .WithReference(orders)
    .WithReference(coupon)
    .WithReference(agentApp)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint)
    .WithEnvironment("OpenIdConnect__Authority", identityEndpoint);

// Feed the Mango.Web redirect URIs to whichever provider is active.
if (useOpenIddict)
{
    identityRef
        .WithEnvironment("OpenIddict__Clients__MangoWeb__RedirectUri", $"{webApp.GetEndpoint("https")}/signin-oidc")
        .WithEnvironment("OpenIddict__Clients__MangoWeb__PostLogoutUri", $"{webApp.GetEndpoint("https")}/signout-callback-oidc");
}
else
{
    identityRef
        .WithEnvironment("IdentityServer__Clients__1__RedirectUris__0", $"{webApp.GetEndpoint("https")}/signin-oidc")
        .WithEnvironment("IdentityServer__Clients__1__PostLogoutRedirectUris__0", $"{webApp.GetEndpoint("https")}/signout-callback-oidc");
}

var sagaOrchestrators = builder.AddProject<Projects.Mango_Orchestrators>("mango-saga-orchestrators")
    .WaitFor(rabbitMq).WithReference(rabbitMq)
    .WaitFor(sagaorchestratorsdb).WithReference(sagaorchestratorsdb);


var gateway = builder.AddProject<Projects.Mango_Gateway>("mango-gateway")
    .WithReference(products)
    .WithReference(orders)
    .WithReference(shoppingcart)
    .WithReference(coupon)
    .WithReference(agentApp);


var mangoUi = builder.AddExecutable("mango-ui", "pnpm", "../UI/mango-ui", "dev")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints();

//var gatewayEndpoint = gateway.GetEndpoint(launchProfileName);
var mangoUiUrl = "http://localhost:5173";
//var gatewayUrl = gatewayEndpoint.Url;

mangoUi
    .WithEnvironment("VITE_IDENTITY_URL", identityEndpoint);

// The SPA client is only seeded by OpenIdentity.App; Duende configures
// its clients statically in Identity.API appsettings.
if (useOpenIddict)
{
    identityRef
        .WithEnvironment("OpenIddict__Clients__MangoSpa__RedirectUri", $"{mangoUiUrl}/callback")
        .WithEnvironment("OpenIddict__Clients__MangoSpa__SilentRedirectUri", $"{mangoUiUrl}/silent-callback")
        .WithEnvironment("OpenIddict__Clients__MangoSpa__PostLogoutUri", mangoUiUrl);
}

// Every .NET service also ships its logs and metrics to the collector. Kept in one
// place so a new service only has to be added to this list.
IResourceBuilder<ProjectResource>[] instrumentedProjects =
[
    identityRef, products, coupon, shoppingcart, orders, payments,
    agentApp, webApp, sagaOrchestrators, gateway
];

foreach (var project in instrumentedProjects)
{
    project.WithGrafanaTelemetry(observability);
}

builder.Build().Run();

static bool ShouldUseHttpForEndpoints()
{
    const string envVar = "ASPIRE_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(envVar);
    return envValue is "true" or "1";
}
