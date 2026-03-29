var builder = DistributedApplication.CreateBuilder(args);


var postgresPassword = builder.AddParameter("postgres-password", "postgres");
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", "YourSecretPassword");

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithBindMount("./init-scripts/productdb", "/docker-entrypoint-initdb.d");

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
    .WithManagementPlugin(port: 15672);

var debezium = builder.AddContainer("debezium", "debezium/server", "2.7.3.Final")
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "api")
    .WithBindMount("./init-configs/products/application.properties", "/debezium/conf/application.properties")
    .WithVolume("debezium-data", "/debezium/data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithReference(productdb).WaitFor(productdb)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // PostgreSQL connection
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_HOSTNAME", postgres.Resource.Name)
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_PORT", postgres.Resource.Port)
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_USER", "postgres")
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_PASSWORD", "postgres")
    .WithEnvironment("DEBEZIUM_SOURCE_DATABASE_DBNAME", "productdb")
    // RabbitMQ connection
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_HOST", rabbitMq.Resource.Name)
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_PORT", "5672")
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_USERNAME", "guest")
    .WithEnvironment("DEBEZIUM_SINK_RABBITMQ_CONNECTION_PASSWORD", "YourSecretPassword");

var identityType = (Environment.GetEnvironmentVariable("IdentityType")
        ?? Environment.GetEnvironmentVariable("IDENTITY_TYPE")
        ?? "Duende")
    .Trim();
var useOpenIddict = identityType.Equals("OpenIddict", StringComparison.OrdinalIgnoreCase);

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

var identity = builder.AddProject<Projects.Identity_API>("identity-app")
    .WaitFor(identitydb)
    .WithReference(identitydb);

var openIdentity = builder.AddProject<Projects.OpenIdentity_App>("openidentity-app")
    .WaitFor(openidentitydb)
    .WithReference(openidentitydb);

var identityRef = useOpenIddict ? openIdentity : identity;

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


var agentApp = builder.AddProject<Projects.ChatAgent_App>("chatagent-app")
    .WithReference(chatagentdb).WaitFor(chatagentdb)
    .WithReference(identityRef).WaitFor(identityRef)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint)
    .WithReference(coupon).WaitFor(coupon)
    .WithReference(shoppingcart).WaitFor(shoppingcart)
    .WithReference(products).WaitFor(products);


var webApp = builder.AddProject<Projects.Mango_Web>("mango-web")
    .WithReference(identityRef)
    .WithReference(products)
    .WithReference(shoppingcart)
    .WithReference(orders)
    .WithReference(coupon)
    .WithReference(agentApp)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint)
    .WithEnvironment("OpenIdConnect__Authority", identityEndpoint);

identity.WithEnvironment("IdentityServer__Clients__1__RedirectUris__0", $"{webApp.GetEndpoint("https")}/signin-oidc")
        .WithEnvironment("IdentityServer__Clients__1__PostLogoutRedirectUris__0", $"{webApp.GetEndpoint("https")}/signout-callback-oidc");

openIdentity.WithEnvironment("OpenIddict__Clients__MangoWeb__RedirectUri", $"{webApp.GetEndpoint("https")}/signin-oidc")
    .WithEnvironment("OpenIddict__Clients__MangoWeb__PostLogoutUri", $"{webApp.GetEndpoint("https")}/signout-callback-oidc");

builder.AddProject<Projects.Mango_Orchestrators>("mango-saga-orchestrators")
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

openIdentity
    .WithEnvironment("OpenIddict__Clients__MangoSpa__RedirectUri", $"{mangoUiUrl}/callback")
    .WithEnvironment("OpenIddict__Clients__MangoSpa__SilentRedirectUri", $"{mangoUiUrl}/silent-callback")
    .WithEnvironment("OpenIddict__Clients__MangoSpa__PostLogoutUri", mangoUiUrl);

builder.Build().Run();

static bool ShouldUseHttpForEndpoints()
{
    const string envVar = "ASPIRE_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(envVar);
    return envValue is "true" or "1";
}
