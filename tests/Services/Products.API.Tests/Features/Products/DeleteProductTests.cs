using Mango.Core.Exceptions;

namespace Products.API.Tests.Features.Products;

public class DeleteProductTests
{
    private readonly ProductDbContext _dbContext;

    public DeleteProductTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ProductExists_Then_DeletesProductAndReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "To Delete",
            Description = "D1",
            CategoryName = "C1",
            ImageUrl = "I1",
            AvailableStock = 10
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteProduct.Command.Handler(_dbContext);
        var command = new DeleteProduct.Command { Id = productId };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeTrue();

        var deletedProduct = await _dbContext.Products.FindAsync(productId);
        deletedProduct.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_When_ProductDoesNotExist_Then_ThrowDataVerificationException()
    {
        // Arrange
        var handler = new DeleteProduct.Command.Handler(_dbContext);
        var command = new DeleteProduct.Command { Id = Guid.NewGuid() };

        // Act && Assert
        var act = await Assert.ThrowsAsync<DataVerificationException>(() => handler.HandleAsync(command, CancellationToken.None));
    }
}
