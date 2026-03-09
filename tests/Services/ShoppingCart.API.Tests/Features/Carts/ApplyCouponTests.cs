namespace ShoppingCart.API.Tests.Features.Carts;

public class ApplyCouponTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly Mock<ICouponsApi> _couponsApiMock;
    private readonly ShoppingCartDbContext _dbContext;

    public ApplyCouponTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _couponsApiMock = new Mock<ICouponsApi>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CouponValid_Then_UpdatesCartHeader()
    {
        // Arrange
        var userId = "test-user";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var header = new CartHeader { Id = Guid.NewGuid(), UserId = userId, CouponCode = "" };
        _dbContext.CartHeaders.Add(header);
        await _dbContext.SaveChangesAsync();

        _couponsApiMock.Setup(api => api.GetCouponAsync("SAVE10"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(new CouponDto { Code = "SAVE10", DiscountAmount = 10 }));

        var handler = new ApplyCoupon.Handler(_dbContext, _currentUserContextMock.Object);
        var command = new ApplyCoupon.Command { CouponCode = "SAVE10" };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        var updatedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        updatedHeader!.CouponCode.ShouldBe("SAVE10");
    }
}
