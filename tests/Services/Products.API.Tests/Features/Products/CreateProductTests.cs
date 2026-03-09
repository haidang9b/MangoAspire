namespace Products.API.Tests.Features.Products;

public class CreateProductTests
{
    private readonly ProductDbContext _dbContext;

    public CreateProductTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CommandIsValid_Then_SavesProductAndReturnsId()
    {
        // Arrange
        var handler = new CreateProduct.Command.Handler(_dbContext);
        var command = new CreateProduct.Command
        {
            Name = "New Product",
            Price = 9.99m,
            Description = "A great product",
            CategoryName = "Electronics",
            CatalogTypeId = 1,
            ImageUrl = "http://example.com/image.png",
            Stock = 100
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldNotBe(Guid.Empty);

        var savedProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == result.Data);
        savedProduct.ShouldNotBeNull();
        savedProduct.Name.ShouldBe("New Product");
        savedProduct.Price.ShouldBe(9.99m);
        savedProduct.AvailableStock.ShouldBe(100);

    }
}
