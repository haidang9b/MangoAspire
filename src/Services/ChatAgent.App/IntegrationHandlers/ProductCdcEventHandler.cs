using ChatAgent.App.Cdc;
using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatAgent.App.IntegrationHandlers;

/// <summary>
/// Keeps the local product read-model and its retrieval index in step with Products.API.
/// The mirror row and the index entry are committed together, so the agent can never see a
/// product that has no searchable text (or vice versa).
/// </summary>
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
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

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
                UpdatedAt = DateTime.UtcNow,
            };

            _dbContext.Products.Add(product);
            _logger.LogInformation("CDC: created product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }
        else
        {
            product.Name = cdcEvent.Name;
            product.Description = cdcEvent.Description;
            product.CategoryName = cdcEvent.CategoryName;
            product.ImageUrl = cdcEvent.ImageUrl;
            product.Price = cdcEvent.Price;
            product.CatalogTypeId = cdcEvent.CatalogTypeId;
            product.UpdatedAt = DateTime.UtcNow;

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

    private async Task HandleDeleteAsync(ProductCdcEvent cdcEvent)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        if (product is null)
        {
            return;
        }

        _dbContext.Products.Remove(product);
        await _vectorIndexer.RemoveAsync(VectorSourceType.Product, cdcEvent.ProductId.ToString());
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("CDC: deleted product {ProductId}", cdcEvent.ProductId);
    }

    /// <summary>
    /// Name and category lead so that both the embedding and the tsvector weight them
    /// ahead of the long-tail description text.
    /// </summary>
    private static string BuildSearchableText(Product product)
        => $"{product.Name}\nCategory: {product.CategoryName}\n{product.Description}";
}
