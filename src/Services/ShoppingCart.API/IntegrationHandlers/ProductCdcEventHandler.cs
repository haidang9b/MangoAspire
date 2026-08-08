using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Cdc;

namespace ShoppingCart.API.IntegrationHandlers;

/// <summary>
/// Keeps the local product read-model in step with Products.API.
/// </summary>
/// <remarks>
/// The CDC log is replayable, so this handler must be safe to run against old records: every
/// path fences on <c>SourceLsn</c> and skips anything that would move the row backwards.
/// Deletes tombstone the row instead of removing it, so the watermark survives.
/// </remarks>
public class ProductCdcEventHandler : IIntegrationEventHandler<ProductCdcEvent>
{
    private readonly ShoppingCartDbContext _dbContext;
    private readonly ILogger<ProductCdcEventHandler> _logger;

    public ProductCdcEventHandler(ShoppingCartDbContext dbContext, ILogger<ProductCdcEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCdcEvent cdcEvent)
    {
        _logger.LogInformation("Handling CDC event for ProductId: {ProductId}", cdcEvent.ProductId);

        if (cdcEvent.IsDeleted)
        {
            await HandleDeleteAsync(cdcEvent);
        }
        else
        {
            await HandleUpsertAsync(cdcEvent);
        }
    }

    private async Task HandleUpsertAsync(ProductCdcEvent cdcEvent)
    {
        var existingProduct = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        if (existingProduct == null)
        {
            // Create new product
            var product = new Product
            {
                Id = cdcEvent.ProductId,
                Name = cdcEvent.Name,
                Price = cdcEvent.Price,
                Description = cdcEvent.Description,
                CategoryName = cdcEvent.CategoryName,
                ImageUrl = cdcEvent.ImageUrl,
                SourceLsn = cdcEvent.SourceLsn,
                SourceTimestamp = cdcEvent.SourceTimestamp
            };

            _dbContext.Products.Add(product);
            _logger.LogInformation("CDC: Created product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }
        else
        {
            if (cdcEvent.IsStaleAgainst(existingProduct.SourceLsn, existingProduct.SourceTimestamp))
            {
                _logger.LogDebug(
                    "CDC: skipped stale event for product {ProductId} (incoming LSN {IncomingLsn} <= current {CurrentLsn})",
                    cdcEvent.ProductId, cdcEvent.SourceLsn, existingProduct.SourceLsn);
                return;
            }

            // Update existing product
            existingProduct.Name = cdcEvent.Name;
            existingProduct.Price = cdcEvent.Price;
            existingProduct.Description = cdcEvent.Description;
            existingProduct.CategoryName = cdcEvent.CategoryName;
            existingProduct.ImageUrl = cdcEvent.ImageUrl;
            existingProduct.SourceLsn = cdcEvent.SourceLsn;
            existingProduct.SourceTimestamp = cdcEvent.SourceTimestamp;

            // An upstream re-insert of a previously deleted id clears the tombstone.
            existingProduct.IsDeleted = false;

            _logger.LogInformation("CDC: Updated product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Tombstones the mirror row rather than deleting it. Dropping the row would drop its LSN
    /// watermark with it, and a replayed older insert would then resurrect the product.
    /// Carts that already reference the product keep rendering it; <c>UpsertCart</c> is what
    /// refuses to add a delisted one.
    /// </summary>
    private async Task HandleDeleteAsync(ProductCdcEvent cdcEvent)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        if (product == null)
        {
            return;
        }

        if (cdcEvent.IsStaleAgainst(product.SourceLsn, product.SourceTimestamp))
        {
            _logger.LogDebug(
                "CDC: skipped stale delete for product {ProductId} (incoming LSN {IncomingLsn} <= current {CurrentLsn})",
                cdcEvent.ProductId, cdcEvent.SourceLsn, product.SourceLsn);
            return;
        }

        product.IsDeleted = true;
        product.SourceLsn = cdcEvent.SourceLsn;
        product.SourceTimestamp = cdcEvent.SourceTimestamp;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("CDC: Deleted product {ProductId}", cdcEvent.ProductId);
    }
}
