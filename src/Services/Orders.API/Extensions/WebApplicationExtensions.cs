using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Orders.API.Routes;

namespace Orders.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCurrentUserContext();

        app.MapDefaultEndpoints();
        app.MapOrdersApi();

        return app;
    }


    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}
