using Identity.API.Data;
using Identity.API.Initializer;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseIdentityApiPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }

    public static async Task<WebApplication> InitializeAndMigrateIdentityDbAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initializer = scope.ServiceProvider
            .GetRequiredService<IDBInitializer>();
        await initializer.InitializesAsync();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}
