using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.API.Data;
using Orders.API.Entities;
using Orders.API.Enums;
using Orders.API.Features.Orders;
using Shouldly;

namespace Orders.API.Tests.Features.Orders;

public class CompleteOrderTests
{
    private readonly ILogger<CompleteOrder.Handler> _logger;
    private readonly OrdersDbContext _dbContext;

    public CompleteOrderTests()
    {
        _logger = NullLogger<CompleteOrder.Handler>.Instance;

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_OrderExists_Then_CompletesOrder()
    {
        // Arrange
        var handler = new CompleteOrder.Handler(_dbContext, _logger);
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        // Seed DB
        var orderHeader = new OrderHeader
        {
            Id = orderId,
            UserId = "user123",
            PaymentStatus = false,
            Status = OrderStatus.Processing,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CouponCode = "SAVE10",
            CardNumber = "1234123412341234"
        };
        _dbContext.OrderHeaders.Add(orderHeader);
        await _dbContext.SaveChangesAsync();

        var command = new CompleteOrder.Command
        {
            OrderId = orderId,
            CorrelationId = correlationId
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse(); // Assuming ResultModel IsSuccess exists

        var completedOrder = await _dbContext.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId);
        completedOrder.ShouldNotBeNull();
        completedOrder.PaymentStatus.ShouldBeTrue();
        completedOrder.Status.ShouldBe(OrderStatus.Completed);
    }

    [Fact]
    public async Task HandleAsync_When_OrderDoesNotExist_Then_ReturnsFailure()
    {
        // Arrange
        var handler = new CompleteOrder.Handler(_dbContext, _logger);

        var command = new CompleteOrder.Command
        {
            OrderId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid()
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeTrue(); // Assumes ResultModel has IsError
        result.ErrorMessage.ShouldContain("not found");
    }
}
