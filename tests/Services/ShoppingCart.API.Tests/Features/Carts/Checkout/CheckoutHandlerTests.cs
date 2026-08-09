using Microsoft.Extensions.Logging.Abstractions;
using ShoppingCart.API.Features.Carts.Checkout;

namespace ShoppingCart.API.Tests.Features.Carts;

public class CheckoutHandlerTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly Mock<ICouponsApi> _couponsApiMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly ShoppingCartDbContext _dbContext;

    public CheckoutHandlerTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _couponsApiMock = new Mock<ICouponsApi>();
        _eventBusMock = new Mock<IEventBus>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_ValidCheckoutAndNoCoupon_Then_PublishesEventAndClearsCart()
    {
        // Arrange
        var userId = "checkout-user";
        var cartHeaderId = Guid.NewGuid();
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync(50.00m);

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = cartHeaderId, ProductId = productId, Count = 2 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object, NullLogger<CheckoutHandler>.Instance);

        var requestDto = new CheckoutDto
        {
            CheckoutRequestDto = new CheckoutRequestDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "1234567890",
                CouponCode = "",
                CardNumber = "1234123412341234",
                CVV = "123",
                DiscountTotal = 0,
                ExpiryMonthYear = "12/25",
                OrderTotal = 100.00m,
                PickupDate = DateTime.Now.AddDays(1)
            }
        };

        // Act
        var result = await handler.HandleAsync(requestDto, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        _eventbusMockVerifySuccess(userId, cartHeaderId);

        var headerExists = await _dbContext.CartHeaders.AnyAsync(h => h.Id == cartHeaderId);
        headerExists.ShouldBeFalse();
    }

    private void _eventbusMockVerifySuccess(string userId, Guid cartHeaderId)
    {
        _eventBusMock.Verify(bus => bus.PublishAsync(
           It.Is<CartCheckedOutEvent>(e =>
               e.UserId == userId &&
               e.CartId == cartHeaderId &&
               e.CartTotalItems == 2 &&
               e.OrderTotal == 100.00m)
       ), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_ValidCheckoutWithCoupon_Then_PublishesEventAndClearsCart()
    {
        // Arrange
        var userId = "checkout-user";
        var cartHeaderId = Guid.NewGuid();
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync(100.00m);

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "SAVE10" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = cartHeaderId, ProductId = productId, Count = 1 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        _couponsApiMock.Setup(api => api.GetCouponAsync("SAVE10"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(new CouponDto { Code = "SAVE10", DiscountAmount = 10m }));

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object, NullLogger<CheckoutHandler>.Instance);

        var requestDto = new CheckoutDto
        {
            CheckoutRequestDto = new CheckoutRequestDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                Phone = "0987654321",
                CouponCode = "SAVE10",
                CardNumber = "4321432143214321",
                CVV = "321",
                DiscountTotal = 10m,
                ExpiryMonthYear = "11/25",
                OrderTotal = 90.00m,
                PickupDate = DateTime.Now.AddDays(2)
            }
        };

        // Act
        var result = await handler.HandleAsync(requestDto, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();

        _eventBusMock.Verify(bus => bus.PublishAsync(It.IsAny<CartCheckedOutEvent>()), Times.Once);

        var headerExists = await _dbContext.CartHeaders.AnyAsync(h => h.Id == cartHeaderId);
        headerExists.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_When_ClientInflatesTotals_Then_PublishesServerCalculatedTotals()
    {
        // Arrange - the client claims a bigger discount and a smaller total than the cart supports.
        // This used to be a hard failure only when the coupon lookup happened to return a value;
        // an unknown code skipped the check entirely and the client's numbers were published.
        var userId = "checkout-user";
        var cartHeaderId = Guid.NewGuid();
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync(40.00m);

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "SAVE10" };
        var existingDetail = new CartDetails
        {
            Id = Guid.NewGuid(),
            CartHeaderId = cartHeaderId,
            ProductId = productId,
            Count = 3
        };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        _couponsApiMock.Setup(api => api.GetCouponAsync("SAVE10"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(new CouponDto { Code = "SAVE10", DiscountAmount = 10m }));

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object, NullLogger<CheckoutHandler>.Instance);

        var requestDto = new CheckoutDto
        {
            CheckoutRequestDto = new CheckoutRequestDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@user.com",
                CardNumber = "1",
                CVV = "1",
                ExpiryMonthYear = "1",
                CouponCode = "SAVE10",
                DiscountTotal = 99m,
                OrderTotal = 1m
            }
        };

        // Act
        await handler.HandleAsync(requestDto, CancellationToken.None);

        // Assert - 3 x 40.00 less the coupon's own 10.00, not the 1.00 the client asked for.
        _eventBusMock.Verify(bus => bus.PublishAsync(
            It.Is<CartCheckedOutEvent>(e => e.OrderTotal == 110.00m && e.DiscountTotal == 10m)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_When_CouponNoLongerExists_Then_ThrowsDataVerificationException()
    {
        // Arrange - Coupons.API answers 200 with a null payload for a code it does not know, so
        // this is what a coupon withdrawn between applying and checking out looks like.
        var userId = "checkout-user";
        var cartHeaderId = Guid.NewGuid();
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync(25.00m);

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "GONE" };
        var existingDetail = new CartDetails
        {
            Id = Guid.NewGuid(),
            CartHeaderId = cartHeaderId,
            ProductId = productId,
            Count = 1
        };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        _couponsApiMock.Setup(api => api.GetCouponAsync("GONE"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(null));

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object, NullLogger<CheckoutHandler>.Instance);

        var requestDto = new CheckoutDto
        {
            CheckoutRequestDto = new CheckoutRequestDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@user.com",
                CardNumber = "1",
                CVV = "1",
                ExpiryMonthYear = "1",
                CouponCode = "GONE",
                DiscountTotal = 10m
            }
        };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(requestDto, CancellationToken.None));

        _eventBusMock.Verify(bus => bus.PublishAsync(It.IsAny<CartCheckedOutEvent>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_When_CartNotFound_Then_ThrowsDataVerificationException()
    {
        // Arrange
        _currentUserContextMock.Setup(c => c.UserId).Returns("non-existent");
        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object, NullLogger<CheckoutHandler>.Instance);

        var requestDto = new CheckoutDto
        {
            CheckoutRequestDto = new CheckoutRequestDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@user.com",
                CardNumber = "1",
                CVV = "1",
                ExpiryMonthYear = "1"
            }
        };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(requestDto, CancellationToken.None));
    }

    /// <summary>
    /// Seeds a replicated product. Totals are now computed from these prices rather than taken
    /// from the request, so a cart line without one is not a scenario that can reach the handler.
    /// </summary>
    private async Task<Guid> SeedProductAsync(decimal price)
    {
        var id = Guid.NewGuid();

        _dbContext.Products.Add(new Product
        {
            Id = id,
            Name = "Test Product",
            Price = price,
            Description = "Test Description",
            CategoryName = "Test Category",
            ImageUrl = "http://localhost/test.png"
        });
        await _dbContext.SaveChangesAsync();

        return id;
    }
}
