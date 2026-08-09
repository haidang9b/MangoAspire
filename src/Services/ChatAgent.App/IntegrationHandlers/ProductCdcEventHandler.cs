using ChatAgent.App.Cdc;
using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using ChatAgent.App.Guards.Input;
using EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.IntegrationHandlers;

/// <summary>
/// Keeps the local product read-model and its retrieval index in step with Products.API.
/// The mirror row and the index entry are committed together, so the agent can never see a
/// product that has no searchable text (or vice versa).
/// </summary>
/// <remarks>
/// The CDC log is replayable, so this handler must be safe to run against old records: every
/// path fences on <c>SourceLsn</c> and skips anything that would move the row backwards.
/// Lookups use <c>IgnoreQueryFilters()</c> because a tombstoned row still has to be found —
/// both to keep its watermark and to let a genuine upstream re-insert resurrect it.
/// </remarks>
public class ProductCdcEventHandler : IIntegrationEventHandler<ProductCdcEvent>
{
    private readonly ChatAgentDbContext _dbContext;
    private readonly IVectorIndexer _vectorIndexer;
    private readonly ILogger<ProductCdcEventHandler> _logger;

    public ProductCdcEventHandler(
        ChatAgentDbContext dbContext,
        IVectorIndexer vectorIndexer,
        ILogger<ProductCdcEventHandler> logger)
    {
        _dbContext = dbContext;
        _vectorIndexer = vectorIndexer;
        _logger = logger;
    }

    public async Task HandleAsync(ProductCdcEvent cdcEvent)
    {
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
        var product = await _dbContext.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        // Product text is authored in another service, so it is untrusted here. The row still
        // replicates either way: letting upstream text decide whether a product appears would
        // hand an attacker a way to remove a competitor's dish from the menu by poisoning it.
        var contentFlagged = PromptSecurityScanner.IsSuspicious($"{cdcEvent.Name}\n{cdcEvent.Description}");
        if (contentFlagged)
        {
            _logger.LogWarning(
                "CDC: product {ProductId} carries text matching the injection scanner; replicated with its "
                    + "description withheld from the agent.",
                cdcEvent.ProductId);
        }

        if (product is null)
        {
            product = new Product
            {
                Id = cdcEvent.ProductId,
                Name = cdcEvent.Name,
                Description = cdcEvent.Description,
                CategoryName = cdcEvent.CategoryName,
                ImageUrl = cdcEvent.ImageUrl,
                Price = cdcEvent.Price,
                CatalogTypeId = cdcEvent.CatalogTypeId,
                AvailableStock = cdcEvent.AvailableStock,
                ContentFlagged = contentFlagged,
                UpdatedAt = DateTime.UtcNow,
                SourceLsn = cdcEvent.SourceLsn,
                SourceTimestamp = cdcEvent.SourceTimestamp,
            };

            _dbContext.Products.Add(product);
            _logger.LogInformation("CDC: created product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }
        else
        {
            if (cdcEvent.IsStaleAgainst(product.SourceLsn, product.SourceTimestamp))
            {
                _logger.LogDebug(
                    "CDC: skipped stale event for product {ProductId} (incoming LSN {IncomingLsn} <= current {CurrentLsn})",
                    cdcEvent.ProductId, cdcEvent.SourceLsn, product.SourceLsn);
                return;
            }

            product.Name = cdcEvent.Name;
            product.Description = cdcEvent.Description;
            product.CategoryName = cdcEvent.CategoryName;
            product.ImageUrl = cdcEvent.ImageUrl;
            product.Price = cdcEvent.Price;
            product.CatalogTypeId = cdcEvent.CatalogTypeId;
            product.AvailableStock = cdcEvent.AvailableStock;
            product.ContentFlagged = contentFlagged;
            product.UpdatedAt = DateTime.UtcNow;
            product.SourceLsn = cdcEvent.SourceLsn;
            product.SourceTimestamp = cdcEvent.SourceTimestamp;

            // An upstream re-insert of a previously deleted id clears the tombstone.
            product.IsDeleted = false;

            _logger.LogInformation("CDC: updated product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }

        await _vectorIndexer.UpsertAsync(
            VectorSourceType.Product,
            product.Id.ToString(),
            BuildSearchableText(product),
            new
            {
                product.Name,
                product.CategoryName,
                product.Price,
                product.ImageUrl,
            });

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Tombstones the mirror row rather than deleting it. Dropping the row would drop its LSN
    /// watermark with it, and a replayed older insert would then resurrect the product.
    /// </summary>
    private async Task HandleDeleteAsync(ProductCdcEvent cdcEvent)
    {
        var product = await _dbContext.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        if (product is null)
        {
            // Nothing replicated yet — a delete for an unknown id needs no tombstone, because
            // any later record for it must carry a higher LSN and is safe to apply.
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
        product.UpdatedAt = DateTime.UtcNow;
        product.SourceLsn = cdcEvent.SourceLsn;
        product.SourceTimestamp = cdcEvent.SourceTimestamp;

        await _vectorIndexer.RemoveAsync(VectorSourceType.Product, cdcEvent.ProductId.ToString());
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("CDC: deleted product {ProductId}", cdcEvent.ProductId);
    }

    /// <summary>
    /// Name and category lead so that both the embedding and the tsvector weight them
    /// ahead of the long-tail description text.
    /// </summary>
    /// <remarks>
    /// Stock is deliberately not part of this. <see cref="VectorIndexer"/> nulls a document's
    /// embedding whenever its content changes, so including a value the checkout saga rewrites on
    /// every purchase would mean an embedding call per order — and a window after each one where
    /// the dish is missing from semantic search entirely. It would also buy nothing: nobody
    /// searches for "dishes with seven left".
    /// </remarks>
    private static string BuildSearchableText(Product product)
        => $"{product.Name}\nCategory: {product.CategoryName}\n{product.Description}";
}
