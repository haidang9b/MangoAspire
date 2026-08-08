using EventBus.RabbitMQ;
using ShoppingCart.API.Cdc;
using ShoppingCart.API.Extensions;
using ShoppingCart.API.IntegrationHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// for ServiceBus

//builder.AddServiceBusEventBus("mango")
//    .AddTopic<CartCheckedOutEvent>("checked-out-events");

// Read the product read-model from the mango.cdc.stream log rather than a classic queue, so
// the mirror can be rebuilt by replaying history from a stored offset. See ChatAgent.App.
builder.AddRabbitMQEventBus("eventbus")
    .AddStreamSubscription<ProductCdcEvent, ProductCdcEventHandler>("mango.cdc.stream");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
