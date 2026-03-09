using EventBus.Abstractions;
using Mango.Core.Auth;
using Mango.Core.Domain;
using Mango.Core.Exceptions;
using Mango.Events.Orders;
using Mango.RestApis.Requests;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShoppingCart.API.Data;
using ShoppingCart.API.Dtos;
using ShoppingCart.API.Entities;
using ShoppingCart.API.Features.Carts.Checkout;
using ShoppingCart.API.Services;
using Shouldly;

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

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = cartHeaderId, ProductId = Guid.NewGuid(), Count = 2 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object);

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

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "SAVE10" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = cartHeaderId, ProductId = Guid.NewGuid(), Count = 1 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        _couponsApiMock.Setup(api => api.GetCouponAsync("SAVE10"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(new CouponDto { Code = "SAVE10", DiscountAmount = 10m }));

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object);

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
                DiscountTotal = 10m, // Matches coupon
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
    public async Task HandleAsync_When_CouponDiscountMismatch_Then_ThrowsDataVerificationException()
    {
        // Arrange
        var userId = "checkout-user";
        var cartHeaderId = Guid.NewGuid();
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "SAVE10" };

        _dbContext.CartHeaders.Add(existingHeader);
        await _dbContext.SaveChangesAsync();

        // API says 15m discount
        _couponsApiMock.Setup(api => api.GetCouponAsync("SAVE10"))
            .ReturnsAsync(ResultModel<CouponDto>.Create(new CouponDto { Code = "SAVE10", DiscountAmount = 15m }));

        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object);

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
                DiscountTotal = 10m // User submitted 10m, but API says 15m
            }
        };

        // Act & Assert
        await Should.ThrowAsync<DataVerificationException>(async () =>
            await handler.HandleAsync(requestDto, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_When_CartNotFound_Then_ThrowsDataVerificationException()
    {
        // Arrange
        _currentUserContextMock.Setup(c => c.UserId).Returns("non-existent");
        var handler = new CheckoutHandler(
            _dbContext, _couponsApiMock.Object, _eventBusMock.Object, _currentUserContextMock.Object);

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
}
