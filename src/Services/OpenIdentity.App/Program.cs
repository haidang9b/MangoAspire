using OpenIdentity.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenIdentityServices();

var app = builder.Build();

app.UseOpenIdentityPipeline();

await app.InitializeOpenIdentityDatabaseAsync();

app.Run();
