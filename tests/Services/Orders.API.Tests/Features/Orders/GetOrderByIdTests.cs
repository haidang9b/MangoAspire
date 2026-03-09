using Orders.API.Entities;

namespace Orders.API.Tests.Features.Orders;

public class GetOrderByIdTests
{
    private readonly OrdersDbContext _dbContext;

    public GetOrderByIdTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_OrderExists_Then_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = "user1";
        var order = new OrderHeader
        {
            Id = orderId,
            UserId = userId,
            Status = Enums.OrderStatus.Processing,
            OrderTotal = 100,
            FirstName = "F",
            LastName = "L",
            Email = "E",
            Phone = "P",
            CardNumber = "1234",
            CouponCode = "DISCOUNT10",
            OrderDetails = new List<OrderDetails>()
        };
        _dbContext.OrderHeaders.Add(order);
        await _dbContext.SaveChangesAsync();

        var handler = new GetOrderById.Query.Handler(_dbContext);
        var query = new GetOrderById.Query
        {
            UserId = userId,
            OrderId = orderId,
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(orderId);
    }
}
