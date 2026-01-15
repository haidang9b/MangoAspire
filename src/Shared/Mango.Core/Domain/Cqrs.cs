using MediatR;

namespace Mango.Core.Domain;

public interface ICommand<T> : IRequest<ResultModel<T>>, ITransactionRequest
    where T : notnull
{
}

public interface IQuery<T> : IRequest<ResultModel<T>>
    where T : notnull
{
}

public record ResultModel<T>(T? Data, bool IsError = false, string ErrorMessage = default!) where T : notnull
{
    public static ResultModel<T> Create(T? data, bool isError = false, string errorMessage = default!)
    {
        return new(data, isError, errorMessage);
    }
}
