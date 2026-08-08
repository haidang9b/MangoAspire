using ChatAgent.App.Data;
using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseGlobalExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatAgent API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCurrentUserContext();

        app.MapDefaultEndpoints();

        return app;
    }

    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        using var dbContext = scope.ServiceProvider.GetRequiredService<ChatAgentDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }

    /// <summary>
    /// Ingests the markdown store documents. Unchanged files are skipped by content hash,
    /// so this is cheap on every restart but the first.
    /// </summary>
    /// <remarks>
    /// Chunks are written without embeddings; <see cref="Services.EmbeddingBackfillService"/>
    /// fills those in afterwards, so startup never waits on Azure OpenAI. A failure here is
    /// logged rather than thrown — the agent is still useful without store documents.
    /// </remarks>
    public static async Task<WebApplication> SeedKnowledgeBaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseSeeder>();

        try
        {
            await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
            logger.LogError(ex, "Knowledge base seeding failed; store information will be unavailable.");
        }

        return app;
    }
}
