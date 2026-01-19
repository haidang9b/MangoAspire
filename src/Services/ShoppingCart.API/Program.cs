using ShoppingCart.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
