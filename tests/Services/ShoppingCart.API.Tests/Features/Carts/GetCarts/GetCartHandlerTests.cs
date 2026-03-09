using ShoppingCart.API.Features.Carts.GetCarts;

namespace ShoppingCart.API.Tests.Features.Carts;

public class GetCartHandlerTests
{
    private readonly ShoppingCartDbContext _dbContext;

    public GetCartHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ShoppingCartDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_When_CartExists_Then_ReturnsMappedCartResponse()
    {
        // Arrange
        var userId = "user-cart-123";
        var cartHeaderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var existingHeader = new CartHeader { Id = cartHeaderId, UserId = userId, CouponCode = "TESTCOUPON" };
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Awesome Product",
            Price = 9.99m,
            Description = "A great product",
            CategoryName = "Electronics",
            ImageUrl = "http://example.com/image.png"
        };
        var existingDetail = new CartDetails { Id = Guid.NewGuid(), CartHeaderId = cartHeaderId, ProductId = productId, Count = 3, Product = existingProduct };

        _dbContext.CartHeaders.Add(existingHeader);
        _dbContext.Products.Add(existingProduct); // Seed product table
        _dbContext.CartDetails.Add(existingDetail);
        await _dbContext.SaveChangesAsync();

        var handler = new GetCartHandler(_dbContext);
        var query = new GetCardQuery(userId);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldNotBeNull();

        var cartDto = result.Data;
        cartDto.CartHeader.ShouldNotBeNull();
        cartDto.CartHeader.UserId.ShouldBe(userId);
        cartDto.CartHeader.CouponCode.ShouldBe("TESTCOUPON");

        cartDto.CartDetails.ShouldNotBeNull();
        var detailsList = cartDto.CartDetails as System.Collections.Generic.List<GetCardResponse.CartDetailsResponseDto>;
        detailsList.ShouldNotBeNull();
        detailsList.Count.ShouldBe(1);

        var firstDetail = detailsList![0];
        firstDetail.Count.ShouldBe(3);
        firstDetail.Product.ShouldNotBeNull();
        firstDetail.Product.Name.ShouldBe("Awesome Product");
    }

    [Fact]
    public async Task HandleAsync_When_CartDoesNotExist_Then_ReturnsNull()
    {
        // Arrange
        var handler = new GetCartHandler(_dbContext);
        var query = new GetCardQuery("nonexistent-user");

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsError.ShouldBeFalse();
        result.Data.ShouldBeNull();
    }
}
