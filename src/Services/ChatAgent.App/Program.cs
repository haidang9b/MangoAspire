using ChatAgent.App.Extensions;
using ChatAgent.App.Routes;
using ChatAgent.App.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();
await app.MigrateDatabaseAsync();

app.MapGroup("/api").MapChatRoutes();

app.Run();
