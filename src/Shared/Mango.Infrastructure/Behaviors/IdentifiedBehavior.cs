using Mango.Core.Domain;
using Mango.Infrastructure.ProcessedMessages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mango.Infrastructure.Behaviors;

public class IdentifiedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<IdentifiedBehavior<TRequest, TResponse>> _logger;

    private IProcessedMessageRepository? _processedMessageRepository;

    public IdentifiedBehavior(ILogger<IdentifiedBehavior<TRequest, TResponse>> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _processedMessageRepository = serviceProvider.GetService<IProcessedMessageRepository>();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_processedMessageRepository is null)
        {
            return await next();
        }

        if (request is IIdentifiedCommand identifiedCommand)
        {
            var typeName = typeof(TRequest).FullName!;

            var isProcessed = await _processedMessageRepository.ExistsAsync(identifiedCommand.Id, typeName);

            _logger.LogInformation("Checking if command {CommandType} {CommandId} has been processed", typeName, identifiedCommand.Id);
            if (isProcessed)
            {
                _logger.LogInformation("Command {CommandType} {CommandId} has already been processed", typeName, identifiedCommand.Id);
                return (TResponse)(object)identifiedCommand.CreateDefaultResult();
            }
            var val = await next();

            await _processedMessageRepository.CreateAsync(identifiedCommand.Id, typeName);
            _logger.LogInformation("Command {CommandType} {CommandId} has been processed", typeName, identifiedCommand.Id);
            return val;
        }

        return await next();
    }
}
