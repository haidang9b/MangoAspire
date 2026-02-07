using EventBus.RabbitMQ;
using Mango.Events.Orders;
using Orders.API.Extensions;
using Orders.API.Intergrations.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

//builder.AddServiceBusEventBus("mango")
//    .AddSubscription<CartCheckedOutEvent, CartCheckedOutHandler>("checked-out-events", "checked-out-events-ordersapi")
//    .AddQueue<CreatePaymentRequestCommand>("create-payment-command");

builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<CancelOrderCommand, CancelOrderCommandHandler>("orders-domain.events")
    .AddSubscription<CompleteOrderCommand, CompleteOrderCommandHandler>("orders-domain.events")
    .AddSubscription<CreateOrderCommand, CreateOrderCommandHandler>("orders-domain.events");

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
