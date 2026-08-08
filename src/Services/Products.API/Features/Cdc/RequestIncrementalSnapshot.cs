using Mango.Core.Exceptions;
using System.Text.Json;

namespace Products.API.Features.Cdc;

/// <summary>
/// Asks Debezium to re-read the captured tables and re-emit every row into the CDC stream.
/// </summary>
/// <remarks>
/// This is the deep-backfill path. A consumer normally rebuilds its read-model by replaying
/// the stream from offset 0, but a stream only retains so much history — so a service
/// onboarded long after the fact, or one whose tables were added to the capture list later,
/// needs the source re-read instead.
/// <para>
/// An incremental snapshot does that <b>without</b> dropping the replication slot or the
/// offsets file, so it does not disturb consumers that are already up to date: Debezium
/// interleaves snapshot chunks with live changes and de-duplicates its snapshot window, and
/// consumers fence on <c>__source_lsn</c>, so a re-read row can never overwrite newer state.
/// </para>
/// </remarks>
public class RequestIncrementalSnapshot
{
    public class Command : ICommand<bool>
    {
        /// <summary>
        /// Fully qualified tables to re-read, e.g. <c>public.products</c>. Each must be in the
        /// connector's <c>table.include.list</c> or the signal is ignored.
        /// </summary>
        public required IEnumerable<string> Tables { get; init; }

        public class Validator : AbstractValidator<Command>
        {
            /// <summary>
            /// Only the captured tables are accepted. A typo would otherwise be silently
            /// ignored by the connector, leaving the caller to believe a backfill is running.
            /// </summary>
            private static readonly string[] CapturedTables = ["public.products", "public.catalog_types"];

            public Validator()
            {
                RuleFor(x => x.Tables).NotEmpty();

                RuleForEach(x => x.Tables)
                    .Must(table => CapturedTables.Contains(table))
                    .WithMessage($"Table must be one of: {string.Join(", ", CapturedTables)}.");
            }
        }

        internal class Handler(ProductDbContext dbContext, ILogger<Handler> logger)
            : IRequestHandler<Command, ResultModel<bool>>
        {
            public async Task<ResultModel<bool>> HandleAsync(Command request, CancellationToken cancellationToken)
            {
                var tables = request.Tables.Distinct().ToArray();

                if (tables.Length == 0)
                {
                    throw new DataVerificationException("At least one table must be specified.");
                }

                // Shape is Debezium's: {"data-collections":[...],"type":"incremental"}.
                var payload = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["data-collections"] = tables,
                    ["type"] = "incremental",
                });

                dbContext.DebeziumSignals.Add(new DebeziumSignal
                {
                    // Debezium caps the id at 42 characters; "N" format is 32.
                    Id = Guid.NewGuid().ToString("N"),
                    Type = "execute-snapshot",
                    Data = payload,
                });

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Requested an incremental CDC snapshot of {Tables}. Rows will be re-emitted into the stream; consumers fence on source LSN so up-to-date read-models are unaffected.",
                    string.Join(", ", tables));

                return ResultModel<bool>.Create(true);
            }
        }
    }
}
