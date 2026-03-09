namespace Products.API.Tests.Features.Products;

public class GetProductsTests
{
    private readonly ProductDbContext _dbContext;

    public GetProductsTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
    }

    private async Task SeedData()
    {
        _dbContext.Products.Add(new Product { Id = Guid.NewGuid(), Name = "P1", CategoryName = "Cat1", Price = 10, AvailableStock = 100, Description = "D1", ImageUrl = "I1", CatalogTypeId = 1 });
        _dbContext.Products.Add(new Product { Id = Guid.NewGuid(), Name = "P2", CategoryName = "Cat1", Price = 20, AvailableStock = 100, Description = "D2", ImageUrl = "I2", CatalogTypeId = 1 });
        _dbContext.Products.Add(new Product { Id = Guid.NewGuid(), Name = "P3", CategoryName = "Cat2", Price = 30, AvailableStock = 100, Description = "D3", ImageUrl = "I3", CatalogTypeId = 2 });
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_When_NoFilters_Then_ReturnsAllProducts()
    {
        // Arrange
        await SeedData();
        var handler = new GetProducts.Query.Handler(_dbContext);
        var query = new GetProducts.Query
        {
            Options = new ProductSearchRequestDto
            {
                PageIndex = 0,
                PageSize = 10
            }
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.Data.Count().ShouldBe(3);
        result.Data.Count.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_When_CategoryFilterApplied_Then_ReturnsFilteredProducts()
    {
        // Arrange
        await SeedData();
        var handler = new GetProducts.Query.Handler(_dbContext);
        var query = new GetProducts.Query
        {
            Options = new ProductSearchRequestDto
            {
                PageIndex = 0,
                PageSize = 10,
                CatalogTypeId = 1
            }
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Data.Data.Count().ShouldBe(2);
        result.Data.Data.All(p => p.CatalogTypeId == 1).ShouldBeTrue();
    }

}
