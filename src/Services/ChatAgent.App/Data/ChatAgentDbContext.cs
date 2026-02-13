using ChatAgent.App.Data.Entities;
using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Data;

public class ChatAgentDbContext : AppDbContextBase
{
    public DbSet<ChatMessage> ChatMessages { get; set; }

    public ChatAgentDbContext(DbContextOptions<ChatAgentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatAgentDbContext).Assembly);
    }
}
