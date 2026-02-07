namespace Mango.SagaOrchestrators.Repositories;

public interface ISagaRepository
{
    Task<CheckoutSagaState> GetAsync(Guid correlationId);
    Task SaveAsync(CheckoutSagaState saga);
}
