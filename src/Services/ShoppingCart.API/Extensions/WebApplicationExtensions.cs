using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Routes;

namespace ShoppingCart.API.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public WebApplication UseApiPipeline()
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopping Cart API");
                    options.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseAuthorization();
            app.UseCurrentUserContext();

            app.MapDefaultEndpoints();
            app.MapCartApi();

            return app;
        }

        public async Task<WebApplication> MigrateDatabaseAsync()
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingCartDbContext>();
            await dbContext.Database.MigrateAsync();

            return app;
        }
    }
}
