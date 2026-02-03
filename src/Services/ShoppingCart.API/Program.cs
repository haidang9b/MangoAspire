using EventBus.Extensions;
using EventBus.ServiceBus;
using Mango.Events.Orders;
using ShoppingCart.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();
builder.AddServiceBusEventBus("mango")
    .AddTopic<CartCheckedOutEvent>("checked-out-events");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
