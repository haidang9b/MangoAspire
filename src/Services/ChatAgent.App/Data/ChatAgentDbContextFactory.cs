using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatAgent.App.Data;

public class ChatAgentDbContextFactory : IDesignTimeDbContextFactory<ChatAgentDbContext>
{
    public ChatAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatAgentDbContext>();

        // UseVector must mirror the runtime registration, otherwise the design-time model
        // cannot map the embedding column and migrations fail to scaffold. It also parses
        // the connection string eagerly, so this needs a well-formed placeholder — nothing
        // connects to it during "dotnet ef migrations add".
        optionsBuilder.UseNpgsql(
                "Host=localhost;Database=chatagentdb",
                npgsqlOptions => npgsqlOptions.UseVector())
            .UseSnakeCaseNamingConvention();

        return new ChatAgentDbContext(optionsBuilder.Options);
    }
}
