using Mango.SagaOrchestrators.Data;
using Mango.SagaOrchestrators.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mango.SagaOrchestrators.Repositories;

public class SagaRepository(SagaDbContext dbContext) : ISagaRepository
{
    public async Task<CheckoutSagaState> GetAsync(Guid correlationId)
    {
        return await dbContext.CheckoutSagaStates
            .FirstOrDefaultAsync(s => s.Id == correlationId)
            ?? throw new InvalidOperationException($"Saga not found: {correlationId}");
    }

    public async Task SaveAsync(CheckoutSagaState saga)
    {
        var existing = await dbContext.CheckoutSagaStates
            .FirstOrDefaultAsync(s => s.Id == saga.Id);

        if (existing == null)
        {
            saga.UpdatedDate = DateTime.UtcNow;
            await dbContext.CheckoutSagaStates.AddAsync(saga);
        }
        else
        {
            existing.StatusId = saga.StatusId;
            existing.OrderId = saga.OrderId;
            existing.ContextData = saga.ContextData;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}
