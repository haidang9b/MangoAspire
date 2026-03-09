namespace ShoppingCart.API.Tests.Features.Carts;

public class RemoveCouponTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly ShoppingCartDbContext _dbContext;

    public RemoveCouponTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_HeaderExists_Then_RemovesCouponCode()
    {
        // Arrange
        var userId = "test-user";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var header = new CartHeader { Id = Guid.NewGuid(), UserId = userId, CouponCode = "SAVE10" };
        _dbContext.CartHeaders.Add(header);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveCoupon.Handler(_dbContext, _currentUserContextMock.Object);
        var command = new RemoveCoupon.Command();

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        var updatedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        updatedHeader!.CouponCode.ShouldBe("");
    }
}
