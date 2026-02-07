using Coupons.API.Routes;
using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace Coupons.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();

        app.MapDefaultEndpoints();
        app.MapCouponEndpoints();

        return app;
    }

    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        using var dbContext = scope.ServiceProvider.GetRequiredService<CouponDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}
