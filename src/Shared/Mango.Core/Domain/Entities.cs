namespace Mango.Core.Domain;


public interface IEntity
{
}

public interface IEntity<T> : IEntity
{
    T Id { get; set; }
}

public interface ITransactionRequest
{

}

public abstract class EntityBase<T> : IEntity<T>
{
    public T Id { get; set; } = default!;
}

public abstract class EntityBaseWithAudit<T> : EntityBase<T>
{
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }
}

public interface IActivableEntity
{
    bool IsActive { get; set; }
}

public interface ICreatableByEntity
{
    public Guid CreatedById { get; set; }
}

public interface ICreatableDateEntity
{
    DateTimeOffset CreatedDate { get; set; }
}

public interface IDescriptionableEntity
{
    string Description { get; set; }
}


public interface IUpdatableNullEntity : IUpdatableNullDateEntity
{
    Guid? UpdatedById { get; set; }
}


public interface IUpdatableNullDateEntity
{
    DateTimeOffset? UpdatedDate { get; set; }
}

public interface ITenantableEntity
{
    Guid? RootGroupId { get; set; }
}
