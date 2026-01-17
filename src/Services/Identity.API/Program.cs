using Identity.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddIdentityApiDefaults();

var app = builder.Build();

app.UseIdentityApiPipeline();

await app.InitializeAndMigrateIdentityDbAsync();

app.Run();
