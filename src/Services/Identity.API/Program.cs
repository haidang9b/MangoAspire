using Identity.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("sd.json", optional: false, reloadOnChange: true);
builder.AddIdentityApiDefaults();

var app = builder.Build();

app.UseIdentityApiPipeline();

await app.InitializeAndMigrateIdentityDbAsync();

app.Run();
