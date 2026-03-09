namespace ShoppingCart.API.Tests.Features.Carts;

public class RemoveFromCartTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly ShoppingCartDbContext _dbContext;

    public RemoveFromCartTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_LastItemInCart_Then_RemovesHeaderAndDetails()
    {
        // Arrange
        var userId = "test-user";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var header = new CartHeader { Id = Guid.NewGuid(), UserId = userId };
        var detail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = header.Id, ProductId = Guid.NewGuid(), Count = 1 };

        _dbContext.CartHeaders.Add(header);
        _dbContext.CartDetails.Add(detail);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveFromCart.Handler(_dbContext);
        var command = new RemoveFromCart.Command { CartDetailsId = detail.Id };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        (await _dbContext.CartHeaders.AnyAsync(h => h.UserId == userId)).ShouldBeFalse();
        (await _dbContext.CartDetails.AnyAsync(d => d.Id == detail.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_When_MultipleItemsInCart_Then_RemovesOnlySelectedDetail()
    {
        // Arrange
        var userId = "test-user";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var header = new CartHeader { Id = Guid.NewGuid(), UserId = userId };
        var detail1 = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = header.Id, ProductId = Guid.NewGuid(), Count = 1 };
        var detail2 = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = header.Id, ProductId = Guid.NewGuid(), Count = 1 };

        _dbContext.CartHeaders.Add(header);
        _dbContext.CartDetails.Add(detail1);
        _dbContext.CartDetails.Add(detail2);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveFromCart.Handler(_dbContext);
        var command = new RemoveFromCart.Command { CartDetailsId = detail1.Id };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        (await _dbContext.CartHeaders.AnyAsync(h => h.UserId == userId)).ShouldBeTrue();
        (await _dbContext.CartDetails.AnyAsync(d => d.Id == detail1.Id)).ShouldBeFalse();
        (await _dbContext.CartDetails.AnyAsync(d => d.Id == detail2.Id)).ShouldBeTrue();
    }
}
