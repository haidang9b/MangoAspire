using EventBus.RabbitMQ;
using Mango.SagaOrchestrators.Extensions;
using Mango.SagaOrchestrators.IntegrationHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// Register saga dependencies
builder.Services.AddScoped<ISagaRepository, SagaRepository>();
builder.Services.AddScoped<ICheckoutSagaOrchestrator, CheckoutSagaOrchestrator>();

builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<CartCheckedOutEvent, CartCheckedOutEventHandler>("carts.events")
    .AddSubscription<OrderCreatedEvent, OrderCreatedEventHandler>("orders.events")
    .AddSubscription<StockReservedEvent, StockReservedEventHandler>("catalog.events")
    .AddSubscription<StockReservationFailedEvent, StockReservationFailedEventHandler>("catalog.events")
    .AddSubscription<PaymentSucceededEvent, PaymentSucceededEventHandler>("payments.events")
    .AddSubscription<PaymentFailedEvent, PaymentFailedEventHandler>("payments.events");

var app = builder.Build();

app.UseApiPipeline();
await app.MigrateDatabaseAsync();

app.Run();
