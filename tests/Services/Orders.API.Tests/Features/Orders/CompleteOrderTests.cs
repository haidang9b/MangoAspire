using Orders.API.Entities;

namespace Orders.API.Tests.Features.Orders;

public class CompleteOrderTests
{
    private readonly OrdersDbContext _dbContext;
    private readonly ILogger<CompleteOrder.Handler> _logger = NullLogger<CompleteOrder.Handler>.Instance;

    public CompleteOrderTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_OrderExists_Then_UpdatesStatusToCompleted()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new OrderHeader
        {
            Id = orderId,
            UserId = "user1",
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

        var handler = new CompleteOrder.Handler(_dbContext, _logger);
        var command = new CompleteOrder.Command
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = orderId
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        var updatedOrder = await _dbContext.OrderHeaders.FindAsync(orderId);
        updatedOrder!.Status.ShouldBe(Enums.OrderStatus.Completed);
    }
}
