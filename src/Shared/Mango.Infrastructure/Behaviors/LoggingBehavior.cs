using Mango.Core.Extensions;
using Mediator.Abstractions;
using Microsoft.Extensions.Logging;

namespace Mango.Infrastructure.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling command {CommandName} ({@Command})", request.GetGenericTypeName(), request);
        TResponse val = await next();
        _logger.LogInformation("Command {CommandName} handled - response: {@Response}", request.GetGenericTypeName(), val);
        return val;
    }
}
