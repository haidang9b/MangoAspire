using Mango.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Orders.API.Services;
using System.Data;
using System.Text.Json;

namespace Orders.API.Behaviours;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    private readonly IDbFacadeResolver _dbFacadeResolver;

    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    private readonly IIntegrationEventService _integrationEventService;

    public TransactionBehavior(
        IDbFacadeResolver dbFacadeResolver,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IIntegrationEventService integrationEventService
    )
    {
        _dbFacadeResolver = dbFacadeResolver ?? throw new ArgumentNullException(nameof(dbFacadeResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _integrationEventService = integrationEventService ?? throw new ArgumentNullException(nameof(integrationEventService));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionRequest)
        {
            return await next();
        }

        _logger.LogInformation("{Prefix} Handled command {MediatRRequest}", nameof(TransactionBehavior<TRequest, TResponse>), typeof(TRequest).FullName);
        _logger.LogDebug("{Prefix} Handled command {MediatRRequest} with content {RequestContent}", nameof(TransactionBehavior<TRequest, TResponse>), typeof(TRequest).FullName, JsonSerializer.Serialize(request));
        _logger.LogInformation("{Prefix} Open the transaction for {MediatRRequest}", nameof(TransactionBehavior<TRequest, TResponse>), typeof(TRequest).FullName);

        if (_dbFacadeResolver.Database.CurrentTransaction != null)
        {
            return await next();
        }

        var strategy = _dbFacadeResolver.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var response = default(TResponse);
            Guid transactionId;
            var typeName = typeof(TRequest).Name;

            // Achieving atomicity
            await using var transaction = await _dbFacadeResolver.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            using (_logger.BeginScope(new List<KeyValuePair<string, object>> { new("TransactionContext", transaction.TransactionId) }))
            {
                _logger.LogInformation("Begin transaction {TransactionId} for {CommandName} ({@Command})", transaction.TransactionId, typeName, request);

                response = await next();

                _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                await transaction.CommitAsync(cancellationToken);

                transactionId = transaction.TransactionId;
            }

            await _integrationEventService.PublishEventsThroughEventBusAsync(transactionId);

            return response;
        });
    }
}
