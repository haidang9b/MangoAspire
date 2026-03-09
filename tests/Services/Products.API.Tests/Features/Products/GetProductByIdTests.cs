using Mango.Core.Exceptions;

namespace Products.API.Tests.Features.Products;

public class GetProductByIdTests
{
    private readonly ProductDbContext _dbContext;

    public GetProductByIdTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ProductExists_Then_ReturnsProductData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Existing Product",
            Description = "D1",
            CategoryName = "C1",
            ImageUrl = "I1",
            AvailableStock = 10,
            Price = 100
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProductById.Query.Handler(_dbContext);
        var query = new GetProductById.Query { ProductId = productId };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(productId);
        result.Data.Name.ShouldBe("Existing Product");
    }

    [Fact]
    public async Task HandleAsync_When_ProductNotFound_Then_ReturnsError()
    {
        // Arrange
        var handler = new GetProductById.Query.Handler(_dbContext);
        var query = new GetProductById.Query
        {
            ProductId = Guid.NewGuid()
        };

        // Act && Assert
        await Assert.ThrowsAsync<DataVerificationException>(() => handler.HandleAsync(query, CancellationToken.None));

    }
}
