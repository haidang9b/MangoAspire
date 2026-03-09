namespace Products.API.Tests.Features.Products;

public class ReserveProductStockTests
{
    private readonly ProductDbContext _dbContext;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly ILogger<ReserveProductStock.Handler> _logger;

    public ReserveProductStockTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
        _eventBusMock = new Mock<IEventBus>();
        _logger = NullLogger<ReserveProductStock.Handler>.Instance;
    }

    [Fact]
    public async Task HandleAsync_When_StockAvailable_Then_DecreasesStockAndPublishesEvent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "P1",
            Description = "D1",
            CategoryName = "C1",
            ImageUrl = "I1",
            AvailableStock = 10
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new ReserveProductStock.Handler(_dbContext, _eventBusMock.Object, _logger);
        var command = new ReserveProductStock.Command
        {
            CorrelationId = correlationId,
            Items = new List<ReserveProductStock.Command.StockItem>
            {
                new(productId, 4)
            }
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeTrue();

        var updatedProduct = await _dbContext.Products.FindAsync(productId);
        updatedProduct!.AvailableStock.ShouldBe(6);

        _eventBusMock.Verify(x => x.PublishAsync(It.Is<StockReservedEvent>(e => e.CorrelationId == correlationId)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_StockInsufficient_Then_ReturnsErrorAndPublishesFailedEvent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "P1",
            Description = "D1",
            CategoryName = "C1",
            ImageUrl = "I1",
            AvailableStock = 2
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var handler = new ReserveProductStock.Handler(_dbContext, _eventBusMock.Object, _logger);
        var command = new ReserveProductStock.Command
        {
            CorrelationId = correlationId,
            Items = new List<ReserveProductStock.Command.StockItem>
            {
                new(productId, 5)
            }
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBeFalse();
        result.IsError.ShouldBeTrue();

        _eventBusMock.Verify(x => x.PublishAsync(It.Is<StockReservationFailedEvent>(e => e.CorrelationId == correlationId)), Times.Once);
    }
}
