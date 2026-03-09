using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Entities;
using ShoppingCart.API.Features.Carts;
using Shouldly;

namespace ShoppingCart.API.Tests.Features.Carts;

public class RemoveFromCartTests
{
    private readonly ShoppingCartDbContext _dbContext;

    public RemoveFromCartTests()
    {
        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CartHasMultipleItems_Then_RemovesOnlyDetails()
    {
        // Arrange
        var cartHeaderId = Guid.NewGuid();
        var detailsIdToRemove = Guid.NewGuid();
        var otherDetailsId = Guid.NewGuid();

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = "user-1", CouponCode = "" };

        var detailToRemove = new CartDetails { Id = detailsIdToRemove, CartHeaderId = cartHeaderId, ProductId = Guid.NewGuid(), Count = 1 };
        var otherDetail = new CartDetails { Id = otherDetailsId, CartHeaderId = cartHeaderId, ProductId = Guid.NewGuid(), Count = 2 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.AddRange(detailToRemove, otherDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveFromCart.Handler(_dbContext);
        var command = new RemoveFromCart.Command { CartDetailsId = detailsIdToRemove };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var headerStillExists = await _dbContext.CartHeaders.AnyAsync(h => h.Id == cartHeaderId);
        headerStillExists.ShouldBeTrue();

        var detailRemoved = await _dbContext.CartDetails.FirstOrDefaultAsync(d => d.Id == detailsIdToRemove);
        detailRemoved.ShouldBeNull();

        var otherDetailStillExists = await _dbContext.CartDetails.AnyAsync(d => d.Id == otherDetailsId);
        otherDetailStillExists.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_When_CartHasOneItem_Then_RemovesBothHeaderAndDetails()
    {
        // Arrange
        var cartHeaderId = Guid.NewGuid();
        var detailsIdToRemove = Guid.NewGuid();

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = "user-2", CouponCode = "" };
        var detailToRemove = new CartDetails { Id = detailsIdToRemove, CartHeaderId = cartHeaderId, ProductId = Guid.NewGuid(), Count = 5 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(detailToRemove);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveFromCart.Handler(_dbContext);
        var command = new RemoveFromCart.Command { CartDetailsId = detailsIdToRemove };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var headerExists = await _dbContext.CartHeaders.AnyAsync(h => h.Id == cartHeaderId);
        headerExists.ShouldBeFalse(); // Header should be removed because it was the last item

        var detailExists = await _dbContext.CartDetails.AnyAsync(d => d.Id == detailsIdToRemove);
        detailExists.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_When_CartDetailsNotFound_Then_ThrowsDataVerificationException()
    {
        // Arrange
        var handler = new RemoveFromCart.Handler(_dbContext);
        var command = new RemoveFromCart.Command { CartDetailsId = Guid.NewGuid() };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(command, CancellationToken.None));
    }
}
