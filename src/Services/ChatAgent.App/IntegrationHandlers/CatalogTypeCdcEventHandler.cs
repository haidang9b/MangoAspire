using ChatAgent.App.Cdc;
using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.IntegrationHandlers;

/// <summary>
/// Keeps the local product-category read-model and its retrieval index in step with the
/// upstream <c>catalog_types</c> table.
/// </summary>
public class CatalogTypeCdcEventHandler : IIntegrationEventHandler<CatalogTypeCdcEvent>
{
    private readonly ChatAgentDbContext _dbContext;
    private readonly IVectorIndexer _vectorIndexer;
    private readonly ILogger<CatalogTypeCdcEventHandler> _logger;

    public CatalogTypeCdcEventHandler(
        ChatAgentDbContext dbContext,
        IVectorIndexer vectorIndexer,
        ILogger<CatalogTypeCdcEventHandler> logger)
    {
        _dbContext = dbContext;
        _vectorIndexer = vectorIndexer;
        _logger = logger;
    }

    public async Task HandleAsync(CatalogTypeCdcEvent cdcEvent)
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

    private async Task HandleUpsertAsync(CatalogTypeCdcEvent cdcEvent)
    {
        var category = await _dbContext.ProductCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cdcEvent.CatalogTypeId);

        if (category is null)
        {
            category = new ProductCategory
            {
                Id = cdcEvent.CatalogTypeId,
                Name = cdcEvent.Type,
                UpdatedAt = DateTime.UtcNow,
                SourceLsn = cdcEvent.SourceLsn,
                SourceTimestamp = cdcEvent.SourceTimestamp,
            };

            _dbContext.ProductCategories.Add(category);
            _logger.LogInformation("CDC: created category {CategoryId} - {CategoryName}", cdcEvent.CatalogTypeId, cdcEvent.Type);
        }
        else
        {
            if (cdcEvent.IsStaleAgainst(category.SourceLsn, category.SourceTimestamp))
            {
                _logger.LogDebug(
                    "CDC: skipped stale event for category {CategoryId} (incoming LSN {IncomingLsn} <= current {CurrentLsn})",
                    cdcEvent.CatalogTypeId, cdcEvent.SourceLsn, category.SourceLsn);
                return;
            }

            category.Name = cdcEvent.Type;
            category.UpdatedAt = DateTime.UtcNow;
            category.SourceLsn = cdcEvent.SourceLsn;
            category.SourceTimestamp = cdcEvent.SourceTimestamp;
            category.IsDeleted = false;

            _logger.LogInformation("CDC: updated category {CategoryId} - {CategoryName}", cdcEvent.CatalogTypeId, cdcEvent.Type);
        }

        await _vectorIndexer.UpsertAsync(
            VectorSourceType.ProductCategory,
            category.Id.ToString(),
            $"Menu category: {category.Name}",
            new { category.Name });

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Tombstones the mirror row rather than deleting it, so the LSN watermark survives and a
    /// replayed older record cannot resurrect the category.
    /// </summary>
    private async Task HandleDeleteAsync(CatalogTypeCdcEvent cdcEvent)
    {
        var category = await _dbContext.ProductCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cdcEvent.CatalogTypeId);

        if (category is null)
        {
            return;
        }

        if (cdcEvent.IsStaleAgainst(category.SourceLsn, category.SourceTimestamp))
        {
            _logger.LogDebug(
                "CDC: skipped stale delete for category {CategoryId} (incoming LSN {IncomingLsn} <= current {CurrentLsn})",
                cdcEvent.CatalogTypeId, cdcEvent.SourceLsn, category.SourceLsn);
            return;
        }

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        category.SourceLsn = cdcEvent.SourceLsn;
        category.SourceTimestamp = cdcEvent.SourceTimestamp;

        await _vectorIndexer.RemoveAsync(VectorSourceType.ProductCategory, cdcEvent.CatalogTypeId.ToString());
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("CDC: deleted category {CategoryId}", cdcEvent.CatalogTypeId);
    }
}
