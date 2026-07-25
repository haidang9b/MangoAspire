namespace ShoppingCart.API.Tests.Features.Carts;

public class UpsertCartTests
{
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly ShoppingCartDbContext _dbContext;

    public UpsertCartTests()
    {
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    /// <summary>
    /// Adds a product to the locally replicated products table. The handler
    /// requires one to exist, mirroring the foreign key on cart_details.
    /// </summary>
    private async Task<Guid> SeedProductAsync(Guid? productId = null)
    {
        var id = productId ?? Guid.NewGuid();

        _dbContext.Products.Add(new Product
        {
            Id = id,
            Name = "Test Product",
            Price = 9.99m,
            Description = "Test Description",
            CategoryName = "Test Category",
            ImageUrl = "http://localhost/test.png"
        });
        await _dbContext.SaveChangesAsync();

        return id;
    }

    [Fact]
    public async Task HandleAsync_When_NewCart_Then_CreatesHeaderAndDetails()
    {
        // Arrange
        var userId = "test-user-id";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync();

        var handler = new UpsertCart.Handler(_dbContext, _currentUserContextMock.Object);

        var requestDto = new AddToCartRequestDto
        {
            ProductId = productId,
            Count = 2,
            CouponCode = "DISCOUNT10"
        };
        var command = new UpsertCart.Command { Cart = requestDto };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var savedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        savedHeader.ShouldNotBeNull();
        savedHeader.CouponCode.ShouldBe("DISCOUNT10");

        var savedDetails = await _dbContext.CartDetails.FirstOrDefaultAsync(d => d.CartHeaderId == savedHeader.Id);
        savedDetails.ShouldNotBeNull();
        savedDetails.ProductId.ShouldBe(requestDto.ProductId);
        savedDetails.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_When_ExistingCartOldProduct_Then_UpdatesExistingDetailsCount()
    {
        // Arrange
        var userId = "test-user-id";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var productId = await SeedProductAsync();

        var existingHeader = new CartHeader { Id = Guid.NewGuid(), UserId = userId, CouponCode = "OLDCODE" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = existingHeader.Id, ProductId = productId, Count = 1 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new UpsertCart.Handler(_dbContext, _currentUserContextMock.Object);

        var requestDto = new AddToCartRequestDto
        {
            ProductId = productId,
            Count = 3,
            CouponCode = "NEWDISCOUNT"
        };
        var command = new UpsertCart.Command { Cart = requestDto };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var updatedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        updatedHeader.ShouldNotBeNull();
        updatedHeader.CouponCode.ShouldBe("NEWDISCOUNT");

        var updatedDetail = await _dbContext.CartDetails.FirstOrDefaultAsync(d => d.CartHeaderId == updatedHeader.Id && d.ProductId == productId);
        updatedDetail.ShouldNotBeNull();
        updatedDetail.Count.ShouldBe(4); // 1 originally + 3 newly added
    }

    [Fact]
    public async Task HandleAsync_When_ExistingCartNewProduct_Then_CreatesNewDetails()
    {
        // Arrange
        var userId = "test-user-id";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var existingProductId = await SeedProductAsync();
        var newProductId = await SeedProductAsync();

        var existingHeader = new CartHeader { Id = Guid.NewGuid(), UserId = userId, CouponCode = "OLDCODE" };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = existingHeader.Id, ProductId = existingProductId, Count = 1 };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new UpsertCart.Handler(_dbContext, _currentUserContextMock.Object);

        var requestDto = new AddToCartRequestDto
        {
            ProductId = newProductId,
            Count = 5,
            CouponCode = "FREESHIPPING"
        };
        var command = new UpsertCart.Command { Cart = requestDto };

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeTrue();

        var totalDetailsCount = await _dbContext.CartDetails.CountAsync(d => d.CartHeaderId == existingHeader.Id);
        totalDetailsCount.ShouldBe(2);

        var newDetail = await _dbContext.CartDetails.FirstOrDefaultAsync(d => d.CartHeaderId == existingHeader.Id && d.ProductId == newProductId);
        newDetail.ShouldNotBeNull();
        newDetail.Count.ShouldBe(5);

        var updatedHeader = await _dbContext.CartHeaders.FirstOrDefaultAsync(h => h.UserId == userId);
        updatedHeader.CouponCode.ShouldBe("FREESHIPPING");
    }

    [Fact]
    public async Task HandleAsync_When_ProductNotReplicated_Then_ThrowsDataVerificationException()
    {
        // Arrange
        var userId = "test-user-id";
        _currentUserContextMock.Setup(c => c.UserId).Returns(userId);

        var handler = new UpsertCart.Handler(_dbContext, _currentUserContextMock.Object);

        // No product seeded, mirroring a product this service has not received
        // from CDC yet.
        var command = new UpsertCart.Command
        {
            Cart = new AddToCartRequestDto
            {
                ProductId = Guid.NewGuid(),
                Count = 1,
                CouponCode = null
            }
        };

        // Act / Assert
        await Should.ThrowAsync<DataVerificationException>(
            () => handler.HandleAsync(command, CancellationToken.None));

        (await _dbContext.CartHeaders.AnyAsync()).ShouldBeFalse();
        (await _dbContext.CartDetails.AnyAsync()).ShouldBeFalse();
    }
}
