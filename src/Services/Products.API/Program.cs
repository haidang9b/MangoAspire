using Products.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();
