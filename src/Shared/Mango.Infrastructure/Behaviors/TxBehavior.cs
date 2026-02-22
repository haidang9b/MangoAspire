using Mango.Core.Domain;
using Mediator.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Mango.Infrastructure.Behaviors;

public class TxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly IDbFacadeResolver _dbFacadeResolver;

    private readonly ILogger<TxBehavior<TRequest, TResponse>> _logger;

    public TxBehavior(
        IDbFacadeResolver dbFacadeResolver,
        ILogger<TxBehavior<TRequest, TResponse>> logger
    )
    {
        _dbFacadeResolver = dbFacadeResolver ?? throw new ArgumentNullException(nameof(dbFacadeResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionRequest)
        {
            return await next();
        }

        _logger.LogInformation("{Prefix} Handled command {MediatRRequest}", nameof(TxBehavior<TRequest, TResponse>), typeof(TRequest).FullName);
        _logger.LogDebug("{Prefix} Handled command {MediatRRequest} with content {RequestContent}", nameof(TxBehavior<TRequest, TResponse>), typeof(TRequest).FullName, JsonSerializer.Serialize(request));
        _logger.LogInformation("{Prefix} Open the transaction for {MediatRRequest}", nameof(TxBehavior<TRequest, TResponse>), typeof(TRequest).FullName);

        if (_dbFacadeResolver.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var strategy = _dbFacadeResolver.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Achieving atomicity
            await using var transaction = await _dbFacadeResolver.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var response = await next();
            _logger.LogInformation("{Prefix} Executed the {MediatRRequest} request", nameof(TxBehavior<TRequest, TResponse>), typeof(TRequest).FullName);

            await transaction.CommitAsync(cancellationToken);

            return response;
        });
    }
}
