using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatAgent.App.Data;

public class ChatAgentDbContextFactory : IDesignTimeDbContextFactory<ChatAgentDbContext>
{
    public ChatAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatAgentDbContext>();

        optionsBuilder.UseNpgsql("chatagentdb")
            .UseSnakeCaseNamingConvention();

        return new ChatAgentDbContext(optionsBuilder.Options);
    }
}
