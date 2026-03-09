namespace Orders.API.Tests.Features.Orders;

public class CreateOrderTests
{
    private readonly Mock<IIntegrationEventService> _integrationEventServiceMock;
    private readonly OrdersDbContext _dbContext;

    public CreateOrderTests()
    {
        _integrationEventServiceMock = new Mock<IIntegrationEventService>();

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OrdersDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ValidCommand_Then_CreatesOrderAndPublishesEvent()
    {
        // Arrange
        var handler = new CreateOrder.Handler(_dbContext, _integrationEventServiceMock.Object);
        var correlationId = Guid.NewGuid();

        var cartEvent = new CartCheckedOutEvent
        {
            UserId = "user-123",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            CartTotalItems = 2,
            OrderTotal = 100.50m,
            CardNumber = "1234567812345678",
            CouponCode = "SAVE10",
            CVV = "123",
            DiscountTotal = 10m,
            ExpiryMonthYear = "12/25",
            Phone = "1234567890",
            PickupDate = DateTime.Now.AddDays(1),
            CartDetails = new List<CartCheckedOutEvent.CartDetailsDto>
            {
                new CartCheckedOutEvent.CartDetailsDto { ProductId = Guid.NewGuid(), Count = 2 }
            }
        };

        var command = new CreateOrder.Command
        {
            CorrelationId = correlationId,
            Event = cartEvent
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldNotBe(Guid.Empty);

        var savedOrder = await _dbContext.OrderHeaders.Include(x => x.OrderDetails).FirstOrDefaultAsync(x => x.Id == result.Data);
        savedOrder.ShouldNotBeNull();
        savedOrder.UserId.ShouldBe(cartEvent.UserId);
        savedOrder.OrderDetails.ShouldNotBeEmpty();
        savedOrder.OrderDetails.Count.ShouldBe(1);

        _integrationEventServiceMock.Verify(x =>
            x.AddAndSaveEventAsync(It.Is<OrderCreatedEvent>(e => e.CorrelationId == correlationId && e.OrderId == result.Data)),
            Times.Once);
    }
}
