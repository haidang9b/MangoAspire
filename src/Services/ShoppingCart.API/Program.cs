using EventBus.RabbitMQ;
using ShoppingCart.API.Cdc;
using ShoppingCart.API.Extensions;
using ShoppingCart.API.IntegrationHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// for ServiceBus

//builder.AddServiceBusEventBus("mango")
//    .AddTopic<CartCheckedOutEvent>("checked-out-events");

builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<ProductCdcEvent, ProductCdcEventHandler>("mango-cdc-exchange");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
