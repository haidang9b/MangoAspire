using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using OpenIdentity.App.Routes;

namespace OpenIdentity.App.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseOpenIdentityPipeline(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // -------------------------------------------------------
        //  Minimal APIs for Management (Admin Only)
        // -------------------------------------------------------
        app.MapGroup("/api")
           .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
           .MapClientsApi()
           .MapResourcesApi()
           .MapRolesApi();

        return app;
    }

    public static async Task<WebApplication> InitializeOpenIdentityDatabaseAsync(this WebApplication app)
    {
        // Auto-migrate the database and seed on startup.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await db.Database.MigrateAsync();
            await initializer.InitializeAsync();
        }

        return app;
    }
}
