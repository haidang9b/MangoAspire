using ChatAgent.App.Cdc;
using ChatAgent.App.Extensions;
using ChatAgent.App.IntegrationHandlers;
using ChatAgent.App.Routes;
using EventBus.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

// Products and categories are replicated into the local read-model from Debezium change
// records rather than fetched from Products.API on every chat turn.
//
// Read from the mango.cdc.stream log, not a classic queue: the log is retained and ordered,
// so this service reads it from its own stored offset and — with no stored offset — replays
// it from the beginning to rebuild the mirror and its vector index. That is what lets the
// database be dropped, or a brand-new consumer be introduced, without losing history.
builder.AddRabbitMQEventBus("eventbus")
    .AddStreamSubscription<ProductCdcEvent, ProductCdcEventHandler>("mango.cdc.stream")
    .AddStreamSubscription<CatalogTypeCdcEvent, CatalogTypeCdcEventHandler>("mango.cdc.stream");

var app = builder.Build();

app.UseApiPipeline();
await app.MigrateDatabaseAsync();
await app.SeedKnowledgeBaseAsync();

app.MapGroup("/api").MapChatRoutes();

app.Run();
