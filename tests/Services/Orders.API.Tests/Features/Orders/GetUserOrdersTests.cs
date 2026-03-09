using Orders.API.Entities;

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
    public async Task HandleAsync_When_OrdersExistForUser_Then_ReturnsOrders()
    {
        // Arrange
        var userId = "user1";
        _dbContext.OrderHeaders.Add(new OrderHeader
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = Enums.OrderStatus.Processing,
            CardNumber = "1234",
            CouponCode = "DISCOUNT10",
            OrderTotal = 100,
            FirstName = "F",
            LastName = "L",
            Email = "E",
            Phone = "P",
            OrderDetails = new List<OrderDetails>()
        });
        await _dbContext.SaveChangesAsync();

        var handler = new GetUserOrders.Query.Handler(_dbContext);
        var query = new GetUserOrders.Query { UserId = userId };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.Count.ShouldBe(1);
    }
}
