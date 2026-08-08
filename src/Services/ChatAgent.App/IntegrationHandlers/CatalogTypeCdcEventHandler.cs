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
            .FirstOrDefaultAsync(c => c.Id == cdcEvent.CatalogTypeId);

        if (category is null)
        {
            category = new ProductCategory
            {
                Id = cdcEvent.CatalogTypeId,
                Name = cdcEvent.Type,
                UpdatedAt = DateTime.UtcNow,
            };

            _dbContext.ProductCategories.Add(category);
            _logger.LogInformation("CDC: created category {CategoryId} - {CategoryName}", cdcEvent.CatalogTypeId, cdcEvent.Type);
        }
        else
        {
            category.Name = cdcEvent.Type;
            category.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("CDC: updated category {CategoryId} - {CategoryName}", cdcEvent.CatalogTypeId, cdcEvent.Type);
        }

        await _vectorIndexer.UpsertAsync(
            VectorSourceType.ProductCategory,
            category.Id.ToString(),
            $"Menu category: {category.Name}",
            new { category.Name });

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleDeleteAsync(CatalogTypeCdcEvent cdcEvent)
    {
        var category = await _dbContext.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == cdcEvent.CatalogTypeId);

        if (category is null)
        {
            return;
        }

        _dbContext.ProductCategories.Remove(category);
        await _vectorIndexer.RemoveAsync(VectorSourceType.ProductCategory, cdcEvent.CatalogTypeId.ToString());
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("CDC: deleted category {CategoryId}", cdcEvent.CatalogTypeId);
    }
}
