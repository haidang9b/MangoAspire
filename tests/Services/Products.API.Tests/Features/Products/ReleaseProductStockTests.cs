namespace Products.API.Tests.Features.Products;

public class ReleaseProductStockTests
{
    private readonly ProductDbContext _dbContext;
    private readonly ILogger<ReleaseProductStock.Handler> _logger;

    public ReleaseProductStockTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
        _logger = NullLogger<ReleaseProductStock.Handler>.Instance;
    }

    [Fact]
    public async Task HandleAsync_When_ProductsExist_Then_IncreasesStockAndReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "P1",
            Description = "D1",
            CategoryName = "C1",
            ImageUrl = "I1",
            AvailableStock = 10
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new ReleaseProductStock.Handler(_dbContext, _logger);
        var command = new ReleaseProductStock.Command
        {
            CorrelationId = Guid.NewGuid(),
            Items = new List<ReleaseProductStock.Command.StockItem>
            {
                new(productId, 5)
            }
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeTrue();

        var updatedProduct = await _dbContext.Products.FindAsync(productId);
        updatedProduct!.AvailableStock.ShouldBe(15);
    }
}
