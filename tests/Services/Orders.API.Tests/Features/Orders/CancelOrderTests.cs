using Mango.Events.Orders;
using Microsoft.EntityFrameworkCore;
using Moq;
using Orders.API.Data;
using Orders.API.Entities;
using Orders.API.Enums;
using Orders.API.Features.Orders;
using Orders.API.Services;
using Shouldly;

namespace Orders.API.Tests.Features.Orders;

public class CancelOrderTests
{
    private readonly Mock<IIntegrationEventService> _integrationEventServiceMock;
    private readonly OrdersDbContext _dbContext;

    public CancelOrderTests()
    {
        _integrationEventServiceMock = new Mock<IIntegrationEventService>();

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_OrderExists_Then_CancelsOrderAndPublishesEvent()
    {
        // Arrange
        var handler = new CancelOrder.Handler(_dbContext, _integrationEventServiceMock.Object);
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var cancelReason = "Customer requested cancellation";

        // Seed DB
        var orderHeader = new OrderHeader
        {
            Id = orderId,
            UserId = "user123",
            PaymentStatus = true,
            Status = OrderStatus.Processing,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CouponCode = "SAVE10",
            CardNumber = "1234123412341234"
        };
        _dbContext.OrderHeaders.Add(orderHeader);
        await _dbContext.SaveChangesAsync();

        var command = new CancelOrder.Command
        {
            OrderId = orderId,
            CorrelationId = correlationId,
            CancelReason = cancelReason
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse(); // Assuming ResultModel IsSuccess exists

        var cancelledOrder = await _dbContext.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId);
        cancelledOrder.ShouldNotBeNull();
        cancelledOrder.PaymentStatus.ShouldBeFalse();
        cancelledOrder.Status.ShouldBe(OrderStatus.Cancelled);
        cancelledOrder.CancelReason.ShouldBe(cancelReason);

        _integrationEventServiceMock.Verify(x =>
            x.AddAndSaveEventAsync(It.Is<OrderCancelledEvent>(e => e.CorrelationId == correlationId && e.OrderId == orderId)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_OrderDoesNotExist_Then_ReturnsFailure()
    {
        // Arrange
        var handler = new CancelOrder.Handler(_dbContext, _integrationEventServiceMock.Object);
        var orderId = Guid.NewGuid();

        var command = new CancelOrder.Command
        {
            OrderId = orderId,
            CorrelationId = Guid.NewGuid(),
            CancelReason = "Test"
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeTrue(); // Assumes ResultModel has IsError property based on typical error responses
        result.ErrorMessage.ShouldContain("not found");

        _integrationEventServiceMock.Verify(x => x.AddAndSaveEventAsync(It.IsAny<OrderCancelledEvent>()), Times.Never);
    }
}
