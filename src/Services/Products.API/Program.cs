using EventBus.RabbitMQ;
using Mango.Events.Orders;
using Products.API.Extensions;
using Products.API.IntegrationHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<ReleaseProductStockCommand, ReleaseProductStockCommandHandler>("orders-domain.events")
    .AddSubscription<ReserveProductStockCommand, ReserveProductStockCommandHandler>("orders-domain.events");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
