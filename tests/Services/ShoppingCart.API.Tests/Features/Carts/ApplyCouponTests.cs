using Mango.Core.Auth;
using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingCart.API.Data;
using ShoppingCart.API.Entities;
using ShoppingCart.API.Features.Carts;
using Shouldly;

namespace ShoppingCart.API.Tests.Features.Carts;

public class ApplyCouponTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly ShoppingCartDbContext _dbContext;

    public ApplyCouponTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CartExists_Then_AppliesCouponCode()
    {
        // Arrange
        var userId = "test-user-id";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var existingHeader = new CartHeader { Id = Guid.NewGuid(), UserId = userId, CouponCode = "OLD" };
        _dbContext.CartHeaders.Add(existingHeader);
        await _dbContext.SaveChangesAsync();

        var handler = new ApplyCoupon.Handler(_dbContext, _currentUserContextMock.Object);

        var command = new ApplyCoupon.Command { CouponCode = "NEWCOUPON50" };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var updatedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        updatedHeader.ShouldNotBeNull();
        updatedHeader.CouponCode.ShouldBe("NEWCOUPON50");
    }

    [Fact]
    public async Task HandleAsync_When_CartDoesNotExist_Then_ThrowsDataVerificationException()
    {
        // Arrange
        var userId = "non-existent-user";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var handler = new ApplyCoupon.Handler(_dbContext, _currentUserContextMock.Object);

        var command = new ApplyCoupon.Command { CouponCode = "NEWCOUPON50" };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(command, CancellationToken.None));
    }
}
