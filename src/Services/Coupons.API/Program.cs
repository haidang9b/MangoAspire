using Coupons.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();

app.Run();
