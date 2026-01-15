using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Mango.Infrastructure.Interceptors;

public class PerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger _logger;

    private const int Threshold = 1000; // 1 second

    public PerformanceInterceptor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(PerformanceInterceptor));
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Duration.TotalMilliseconds > Threshold)
        {
            LogLongQuery(command, eventData);
        }
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result
    )
    {
        if (eventData.Duration.TotalMilliseconds > Threshold)
        {
            LogLongQuery(command, eventData);
        }
        return base.ReaderExecuted(command, eventData, result);
    }

    private void LogLongQuery(DbCommand command, CommandExecutedEventData eventData)
    {
        _logger.LogWarning($"Duration: {eventData.Duration.TotalMilliseconds} ms. Long query: {command.CommandText}.");
    }
}
