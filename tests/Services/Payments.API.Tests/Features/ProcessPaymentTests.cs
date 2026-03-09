namespace Payments.API.Tests.Features;

public class ProcessPaymentTests
{
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IOptionsMonitor<PaymentOptions>> _optionsMock;
    private readonly ILogger<ProcessPayment.Handler> _logger;

    public ProcessPaymentTests()
    {
        _eventBusMock = new Mock<IEventBus>();
        _optionsMock = new Mock<IOptionsMonitor<PaymentOptions>>();
        _logger = NullLogger<ProcessPayment.Handler>.Instance;
    }

    [Fact]
    public async Task HandleAsync_When_PaymentOptionsSucceed_Then_PublishesSuccessEventAndReturnsTrue()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new PaymentOptions { PaymentSucceeded = true });

        var handler = new ProcessPayment.Handler(_eventBusMock.Object, _optionsMock.Object, _logger);
        var command = new ProcessPayment.Command
        {
            CorrelationId = correlationId,
            OrderId = Guid.NewGuid(),
            OrderTotal = 100,
            CardNumber = "1234",
            CVV = "123",
            ExpiryMonthYear = "12/25",
            Email = "test@test.com"
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        _eventBusMock.Verify(x => x.PublishAsync(It.Is<PaymentSucceededEvent>(e => e.CorrelationId == correlationId)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_PaymentOptionsFail_Then_PublishesFailedEventAndReturnsFalseWithError()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new PaymentOptions { PaymentSucceeded = false });

        var handler = new ProcessPayment.Handler(_eventBusMock.Object, _optionsMock.Object, _logger);
        var command = new ProcessPayment.Command
        {
            CorrelationId = correlationId,
            OrderId = Guid.NewGuid(),
            OrderTotal = 100,
            CardNumber = "1234",
            CVV = "123",
            ExpiryMonthYear = "12/25",
            Email = "test@test.com"
        };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeTrue();
        result.Data.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Payment declined");

        _eventBusMock.Verify(x => x.PublishAsync(It.Is<PaymentFailedEvent>(e => e.CorrelationId == correlationId && e.Reason == "Payment declined")), Times.Once);
    }
}
