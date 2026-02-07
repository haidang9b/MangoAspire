using Mango.Infrastructure;
using Mango.SagaOrchestrators.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mango.SagaOrchestrators.Data;

public class SagaDbContext : AppDbContextBase
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options)
    {
    }

    public DbSet<CheckoutSagaState> CheckoutSagaStates { get; set; }
}
