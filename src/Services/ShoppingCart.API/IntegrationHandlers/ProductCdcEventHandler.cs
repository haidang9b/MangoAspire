using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Cdc;

namespace ShoppingCart.API.IntegrationHandlers;

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
                ImageUrl = cdcEvent.ImageUrl
            };

            _dbContext.Products.Add(product);
            _logger.LogInformation("CDC: Created product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }
        else
        {
            // Update existing product
            existingProduct.Name = cdcEvent.Name;
            existingProduct.Price = cdcEvent.Price;
            existingProduct.Description = cdcEvent.Description;
            existingProduct.CategoryName = cdcEvent.CategoryName;
            existingProduct.ImageUrl = cdcEvent.ImageUrl;

            _logger.LogInformation("CDC: Updated product {ProductId} - {ProductName}", cdcEvent.ProductId, cdcEvent.Name);
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleDeleteAsync(ProductCdcEvent cdcEvent)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == cdcEvent.ProductId);

        if (product != null)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("CDC: Deleted product {ProductId}", cdcEvent.ProductId);
        }
    }
}
