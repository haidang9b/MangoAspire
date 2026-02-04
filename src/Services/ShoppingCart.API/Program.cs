using EventBus.RabbitMQ;
using ShoppingCart.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// for ServiceBus

//builder.AddServiceBusEventBus("mango")
//    .AddTopic<CartCheckedOutEvent>("checked-out-events");

builder.AddRabbitMQEventBus("eventbus");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
