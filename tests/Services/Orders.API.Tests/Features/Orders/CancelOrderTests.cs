using Orders.API.Entities;

namespace Orders.API.Tests.Features.Orders;

public class CancelOrderTests
{
    private readonly OrdersDbContext _dbContext;
    private readonly Mock<IIntegrationEventService> _integrationEventServiceMock;

    public CancelOrderTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
        _integrationEventServiceMock = new Mock<IIntegrationEventService>();
    }

    [Fact]
    public async Task HandleAsync_When_OrderExists_Then_UpdatesStatusToCancelledAndPublishesEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var order = new OrderHeader
        {
            Id = orderId,
            UserId = "user1",
            Status = Enums.OrderStatus.Cancelled,
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

        var handler = new CancelOrder.Handler(_dbContext, _integrationEventServiceMock.Object);
        var command = new CancelOrder.Command { OrderId = orderId, CorrelationId = correlationId };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Data.ShouldBeTrue();
        var updatedOrder = await _dbContext.OrderHeaders.FindAsync(orderId);
        updatedOrder!.Status.ShouldBe(Enums.OrderStatus.Cancelled);

        _integrationEventServiceMock.Verify(x => x.AddAndSaveEventAsync(It.Is<OrderCancelledEvent>(e => e.OrderId == orderId && e.CorrelationId == correlationId)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_OrderDoesNotExist_Then_ReturnsError()
    {
        // Arrange
        var handler = new CancelOrder.Handler(_dbContext, _integrationEventServiceMock.Object);
        var command = new CancelOrder.Command { OrderId = Guid.NewGuid(), CorrelationId = Guid.NewGuid() };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.ErrorMessage.ShouldContain("not found");
    }
}
