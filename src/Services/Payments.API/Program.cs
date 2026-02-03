using EventBus.Extensions;
using EventBus.ServiceBus;
using Mango.Events.Payments;
using Payments.API.Extensions;
using Payments.API.IntegrationHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();
builder.AddServiceBusEventBus("mango")
    .AddConsumer<CreatePaymentRequestCommand, CreatePaymentRequestCommandHandler>("create-payment-command")
    .AddTopic<OrderPaymentSucceededEvent>("order-payment-succeeded-events")
    .AddTopic<OrderPaymentFailedEvent>("order-payment-failed-events");

var app = builder.Build();

app.UseApiPipeline();

app.Run();
