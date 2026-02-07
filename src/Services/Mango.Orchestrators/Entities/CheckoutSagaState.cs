using Mango.Core.Domain;

namespace Mango.SagaOrchestrators.Entities;

public class CheckoutSagaState : EntityBase<Guid>
{
    public Guid CartId { get; set; }

    public Guid? OrderId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime UpdatedDate { get; set; }

    public string ContextData { get; set; } = null!;

    public OrderStatus StatusId { get; set; }
}
