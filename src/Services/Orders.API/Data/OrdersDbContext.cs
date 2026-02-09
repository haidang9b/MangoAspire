using Mango.Infrastructure;
using Mango.Infrastructure.ProcessedMessages;
using Microsoft.EntityFrameworkCore;

namespace Orders.API.Data;

public class OrdersDbContext : AppDbContextBase
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public virtual DbSet<OrderHeader> OrderHeaders { get; set; }

    public virtual DbSet<OrderDetails> OrderDetails { get; set; }

    public virtual DbSet<ProcessedMessage> ProcessedMessages { get; set; }

    public virtual DbSet<IntegrationEventLogEntry> IntegrationEventLogEntry { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        modelBuilder.ApplyProcessedMessageConfiguration();
    }
}
