namespace Mango.Core.Domain;

/// <summary>
/// Non-generic marker interface for idempotent commands.
/// Used for type checking without generic type parameter issues.
/// </summary>
public interface IIdentifiedCommand
{
    Guid Id { get; set; }

    ResultModel<object> CreateDefaultResult();
}

public interface IIdentifiedCommand<T> : ICommand<T>, IIdentifiedCommand where T : notnull
{
    T CreateDefaultResponse();

    ResultModel<object> IIdentifiedCommand.CreateDefaultResult()
        => ResultModel<object>.Create(CreateDefaultResponse());
}
