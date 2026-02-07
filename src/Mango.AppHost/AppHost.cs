var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres");

var postgres = builder.AddPostgres("postgres", port: 5435, password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var productdb = postgres.AddDatabase("productdb");
var orderdb = postgres.AddDatabase("orderdb");
var coupondb = postgres.AddDatabase("coupondb");
var identitydb = postgres.AddDatabase("identitydb");
var shoppingcartdb = postgres.AddDatabase("shoppingcartdb");
var sagaorchestratorsdb = postgres.AddDatabase("sagaorchestratorsdb");

//var debezium = builder.AddContainer("debezium", "debezium/server", "2.7.3.Final")
//    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "api")
//    .WithBindMount("./application.properties", "/debezium/conf/application.properties")
//    .WithVolume("debezium-data", "/debezium/data")
//    .WithLifetime(ContainerLifetime.Persistent)
//    .WaitFor(productdb);

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

var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var identity = builder.AddProject<Projects.Identity_API>("identity-app")
    .WaitFor(identitydb)
    .WithReference(identitydb);

// Get identity endpoint for services that need JWT validation
var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";
var identityEndpoint = identity.GetEndpoint(launchProfileName);

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
    .WithReference(identity)
    .WithReference(coupon)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint);

var orders = builder.AddProject<Projects.Orders_API>("orders-api")
    .WaitFor(orderdb)
    .WaitFor(rabbitMq)
    //.WaitFor(serviceBus)
    .WithReference(orderdb)
    .WithReference(rabbitMq);
//.WithReference(serviceBus);

var payments = builder.AddProject<Projects.Payments_API>("payments-api")
    .WaitFor(rabbitMq)
    .WithReference(rabbitMq);
//.WaitFor(serviceBus)
//.WithReference(serviceBus);


builder.AddProject<Projects.Mango_Web>("mango-web")
    .WithReference(identity)
    .WithReference(products)
    .WithReference(shoppingcart)
    .WithReference(orders)
    .WithReference(coupon)
    .WithEnvironment("ServiceUrls__IdentityApp", identityEndpoint)
    .WithEnvironment("OpenIdConnect__Authority", identityEndpoint);


builder.AddProject<Projects.Mango_SagaOrchestrators>("mango-saga-orchestrators")
    .WaitFor(rabbitMq).WithReference(rabbitMq)
    .WaitFor(sagaorchestratorsdb).WithReference(sagaorchestratorsdb);


builder.Build().Run();

static bool ShouldUseHttpForEndpoints()
{
    const string envVar = "ASPIRE_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(envVar);
    return envValue is "true" or "1";
}
