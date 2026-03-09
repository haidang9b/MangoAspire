namespace Products.API.Tests.Features.Products;

public class UpdateProductTests
{
    private readonly ProductDbContext _dbContext;

    public UpdateProductTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ProductExists_Then_UpdatesValuesAndReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Old Name",
            Description = "Old Desc",
            Price = 10,
            CategoryName = "Category",
            ImageUrl = "OldImg",
            AvailableStock = 10
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateProduct.Command.Handler(_dbContext);
        var command = new UpdateProduct.Command
        {
            Id = productId,
            Name = "New Name",
            Price = 15,
            Description = "New Desc",
            ImageUrl = "NewImg",
            Stock = 20
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeTrue();

        var updatedProduct = await _dbContext.Products.FindAsync(productId);
        updatedProduct!.Name.ShouldBe("New Name");
        updatedProduct.Price.ShouldBe(15);
        updatedProduct.AvailableStock.ShouldBe(20);
    }
}
