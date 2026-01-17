using Coupons.API.Data;
using Coupons.API.Extensions;
using Coupons.API.Routes;
using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource(connectionName: "coupondb");
builder.Services.AddServices(builder.Configuration);

builder.EnrichNpgsqlDbContext<CouponDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "My API V1");
    });
}
app.UseHttpsRedirection();

app.MapDefaultEndpoints();
app.MapCouponEndpoints();

using (var scope = app.Services.CreateScope())
{
    using var dbContext = scope.ServiceProvider.GetRequiredService<CouponDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
