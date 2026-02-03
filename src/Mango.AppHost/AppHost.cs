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

var serviceBus = builder.AddAzureServiceBus("mango")
    .RunAsEmulator();

var checkedOutEventTopic = serviceBus
    .AddServiceBusTopic("checked-out-events")
    .AddServiceBusSubscription("checked-out-events-ordersapi");

var createPaymentRequestQueue = serviceBus
    .AddServiceBusQueue("create-payment-command");

var orderPaymentFailedEventTopic = serviceBus
    .AddServiceBusTopic("order-payment-failed-events")
    .AddServiceBusSubscription("order-payment-failed-events-paymentsapi");

var orderPaymentSucceededEventTopic = serviceBus
    .AddServiceBusTopic("order-payment-succeeded-events")
    .AddServiceBusSubscription("order-payment-succeeded-events-ordersapi");

var identity = builder.AddProject<Projects.Identity_API>("identity-app")
    .WaitFor(identitydb)
    .WithReference(identitydb);

var products = builder.AddProject<Projects.Products_API>("products-api")
    .WaitFor(productdb)
    .WithReference(productdb);

var coupon = builder.AddProject<Projects.Coupons_API>("coupons-api")
    .WaitFor(coupondb)
    .WithReference(coupondb);

var shoppingcart = builder.AddProject<Projects.ShoppingCart_API>("shoppingcart-api")
    .WaitFor(shoppingcartdb)
    .WaitFor(serviceBus)
    .WithReference(shoppingcartdb)
    .WithReference(serviceBus)
    .WithReference(identity)
    .WithReference(coupon);

var orders = builder.AddProject<Projects.Orders_API>("orders-api")
    .WaitFor(orderdb)
    .WaitFor(serviceBus)
    .WithReference(orderdb)
    .WithReference(serviceBus);

var payments = builder.AddProject<Projects.Payments_API>("payments-api")
    .WaitFor(serviceBus)
    .WithReference(serviceBus);


builder.AddProject<Projects.Mango_Web>("mango-web")
    .WithReference(identity)
    .WithReference(products)
    .WithReference(shoppingcart)
    .WithReference(orders)
    .WithReference(coupon);


builder.Build().Run();
