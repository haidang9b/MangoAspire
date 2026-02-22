using Mango.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddGatewayDefaults();

var app = builder.Build();

app.UseGatewayPipeline();

app.Run();
