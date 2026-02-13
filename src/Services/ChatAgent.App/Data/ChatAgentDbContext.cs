using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.Data;

public class ChatAgentDbContext : AppDbContextBase
{
    public ChatAgentDbContext(DbContextOptions<ChatAgentDbContext> options) : base(options)
    {
    }
}
