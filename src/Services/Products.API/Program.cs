using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Products.API.Data;
using Products.API.Extensions;
using Products.API.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource(connectionName: "productdb");
builder.Services.AddServices(builder.Configuration);

builder.EnrichNpgsqlDbContext<ProductDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API");
        options.RoutePrefix = "swagger"; // Access Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();
app.MapProductsApi();

using (var scope = app.Services.CreateScope())
{
    using var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
