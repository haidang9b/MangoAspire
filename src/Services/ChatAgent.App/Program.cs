using ChatAgent.App.Cdc;
using ChatAgent.App.Extensions;
using ChatAgent.App.IntegrationHandlers;
using ChatAgent.App.Routes;
using EventBus.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// Products and categories are replicated into the local read-model from Debezium change
// records rather than fetched from Products.API on every chat turn.
builder.AddRabbitMQEventBus("eventbus")
    .AddSubscription<ProductCdcEvent, ProductCdcEventHandler>("mango-cdc-exchange")
    .AddSubscription<CatalogTypeCdcEvent, CatalogTypeCdcEventHandler>("mango-cdc-exchange");

var app = builder.Build();

app.UseApiPipeline();
await app.MigrateDatabaseAsync();
await app.SeedKnowledgeBaseAsync();

app.MapGroup("/api").MapChatRoutes();

app.Run();
