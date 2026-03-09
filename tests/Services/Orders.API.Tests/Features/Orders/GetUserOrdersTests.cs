using Microsoft.EntityFrameworkCore;
using Orders.API.Data;
using Orders.API.Entities;
using Orders.API.Enums;
using Orders.API.Features.Orders;
using Shouldly;

namespace Orders.API.Tests.Features.Orders;

public class GetUserOrdersTests
{
    private readonly OrdersDbContext _dbContext;

    public GetUserOrdersTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ValidRequest_Then_ReturnsPaginatedOrdersForUser()
    {
        // Arrange
        var handler = new GetUserOrders.Query.Handler(_dbContext);
        var userId = "user123";

        // Add 15 orders for this user, and 5 for another user
        for (int i = 0; i < 15; i++)
        {
            _dbContext.OrderHeaders.Add(new OrderHeader
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderTime = DateTime.UtcNow.AddMinutes(-i),
                OrderTotal = 100m,
                CartTotalItems = 2,
                Status = OrderStatus.Processing,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                CouponCode = "SAVE10",
                CardNumber = "1234123412341234"
            });
        }

        for (int i = 0; i < 5; i++)
        {
            _dbContext.OrderHeaders.Add(new OrderHeader
            {
                Id = Guid.NewGuid(),
                UserId = "otherUser",
                OrderTime = DateTime.UtcNow,
                OrderTotal = 50m,
                CartTotalItems = 1,
                Status = OrderStatus.Processing,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                CouponCode = "SAVE20",
                CardNumber = "4321432143214321"
            });
        }

        await _dbContext.SaveChangesAsync();

        var query = new GetUserOrders.Query
        {
            UserId = userId,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();

        var paginatedItems = result.Data;
        paginatedItems.ShouldNotBeNull();
        paginatedItems.Count.ShouldBe(15);
        paginatedItems.PageIndex.ShouldBe(1);
        paginatedItems.PageSize.ShouldBe(10);
        paginatedItems.Data.Count().ShouldBe(10);

        // Ensure they are ordered descending by time (the first one should be the most recent)
        var firstOrder = paginatedItems.Data.First();
        var secondOrder = paginatedItems.Data.Skip(1).First();
        firstOrder.OrderTime.ShouldBeGreaterThan(secondOrder.OrderTime);
    }

    [Fact]
    public async Task HandleAsync_When_UserHasNoOrders_Then_ReturnsEmptyPagination()
    {
        // Arrange
        var handler = new GetUserOrders.Query.Handler(_dbContext);

        // Add random order
        _dbContext.OrderHeaders.Add(new OrderHeader
        {
            Id = Guid.NewGuid(),
            UserId = "otherUser",
            OrderTime = DateTime.UtcNow,
            OrderTotal = 50m,
            CartTotalItems = 1,
            Status = OrderStatus.Processing,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            CouponCode = "SAVE20",
            CardNumber = "4321432143214321"
        });
        await _dbContext.SaveChangesAsync();

        var query = new GetUserOrders.Query
        {
            UserId = "user404",
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();

        var paginatedItems = result.Data;
        paginatedItems.ShouldNotBeNull();
        paginatedItems.Count.ShouldBe(0);
        paginatedItems.Data.ShouldBeEmpty();
    }
}
