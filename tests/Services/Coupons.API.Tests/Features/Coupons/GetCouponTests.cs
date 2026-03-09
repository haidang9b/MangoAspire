namespace Coupons.API.Tests.Features.Coupons;

public class GetCouponTests
{
    private readonly CouponDbContext _dbContext;

    public GetCouponTests()
    {
        var options = new DbContextOptionsBuilder<CouponDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new CouponDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CouponExists_Then_ReturnsCouponDto()
    {
        // Arrange
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = "SAVE10",
            DiscountAmount = 10
        };
        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync();

        var handler = new GetCoupon.Query.Handler(_dbContext);
        var query = new GetCoupon.Query { Code = "SAVE10" };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldNotBeNull();
        result.Data.Code.ShouldBe("SAVE10");
        result.Data.DiscountAmount.ShouldBe(10);
    }

    [Fact]
    public async Task HandleAsync_When_CouponDoesNotExist_Then_ReturnsNullData()
    {
        // Arrange
        var handler = new GetCoupon.Query.Handler(_dbContext);
        var query = new GetCoupon.Query { Code = "INVALID" };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeNull();
    }
}
