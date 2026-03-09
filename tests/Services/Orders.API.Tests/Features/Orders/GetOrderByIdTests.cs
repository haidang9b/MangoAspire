using Mango.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Orders.API.Data;
using Orders.API.Entities;
using Orders.API.Enums;
using Orders.API.Features.Orders;
using Shouldly;

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
    public async Task HandleAsync_When_OrderExists_Then_ReturnsOrderDetails()
    {
        // Arrange
        var handler = new GetOrderById.Query.Handler(_dbContext);
        var orderId = Guid.NewGuid();
        var userId = "user123";

        var orderHeader = new OrderHeader
        {
            Id = orderId,
            UserId = userId,
            OrderTime = DateTime.UtcNow,
            OrderTotal = 150m,
            Status = OrderStatus.Processing,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CouponCode = "SAVE10",
            CardNumber = "1234123412341234",
            OrderDetails = new System.Collections.Generic.List<OrderDetails>
            {
                new OrderDetails { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Count = 2, Price = 50m, ProductName = "Product A" },
                new OrderDetails { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Count = 1, Price = 50m, ProductName = "Product B" }
            }
        };

        _dbContext.OrderHeaders.Add(orderHeader);
        await _dbContext.SaveChangesAsync();

        var query = new GetOrderById.Query
        {
            OrderId = orderId,
            UserId = userId
        };

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse(); // Assuming ResultModel has IsSuccess
        result.Data.ShouldNotBeNull(); // Assuming ResultModel has Value

        var details = result.Data;
        details.Id.ShouldBe(orderId);
        details.OrderTotal.ShouldBe(150m);
        details.Items.Count.ShouldBe(2);
        details.FirstName.ShouldBe("John");
    }

    [Fact]
    public async Task HandleAsync_When_OrderDoesNotExist_Then_ThrowsDataVerificationException()
    {
        // Arrange
        var handler = new GetOrderById.Query.Handler(_dbContext);

        var query = new GetOrderById.Query
        {
            OrderId = Guid.NewGuid(),
            UserId = "user123"
        };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(query, CancellationToken.None));
    }
}
