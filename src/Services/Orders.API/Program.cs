using EventBus.Extensions;
using EventBus.ServiceBus;
using Mango.Events.Orders;
using Orders.API.Extensions;
using Orders.API.Intergrations.Handlers;

var builder = WebApplication.CreateBuilder(args);


builder.AddApiDefaults();

builder.AddServiceBusEventBus("mango")
    .AddSubscription<CartCheckedOutEvent, CartCheckedOutHandler>("checked-out-events", "checked-out-events-ordersapi");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
