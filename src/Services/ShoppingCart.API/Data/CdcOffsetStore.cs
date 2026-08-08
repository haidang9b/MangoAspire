using EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCart.API.Data;

/// <summary>
/// Stores CDC stream positions in <c>shoppingcartdb</c>, next to the read-model they describe.
/// </summary>
/// <remarks>
/// Keeping the offset in the service's own database is what makes replay operational rather
/// than architectural: <c>DELETE FROM cdc_stream_offsets</c> and restart, and the product
/// mirror rebuilds from the log.
/// </remarks>
public class CdcOffsetStore(ShoppingCartDbContext dbContext) : ICdcOffsetStore
{
    public async Task<long?> GetAsync(string streamName, CancellationToken cancellationToken = default)
    {
        return await dbContext.CdcStreamOffsets
            .AsNoTracking()
            .Where(x => x.StreamName == streamName)
            .Select(x => (long?)x.Offset)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(string streamName, long offset, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.CdcStreamOffsets
            .FirstOrDefaultAsync(x => x.StreamName == streamName, cancellationToken);

        if (existing is null)
        {
            dbContext.CdcStreamOffsets.Add(new CdcStreamOffset
            {
                StreamName = streamName,
                Offset = offset,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Offset = offset;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
