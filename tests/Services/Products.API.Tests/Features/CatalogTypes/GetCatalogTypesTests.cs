using Products.API.Features.CatalogTypes;

namespace Products.API.Tests.Features.CatalogTypes;

public class GetCatalogTypesTests
{
    private readonly ProductDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;

    public GetCatalogTypesTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task HandleAsync_When_CacheIsEmpty_Then_ReturnsFromDbAndSetsCache()
    {
        // Arrange
        var catalogType = new CatalogType { Id = 1, Type = "Type1" };
        _dbContext.CatalogTypes.Add(catalogType);
        await _dbContext.SaveChangesAsync();

        var handler = new GetCatalogTypes.Query.Handler(_dbContext, _memoryCache);
        var query = new GetCatalogTypes.Query();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Data.Count.ShouldBe(1);
        result.Data[0].Type.ShouldBe("Type1");
    }

    [Fact]
    public async Task HandleAsync_When_CacheHasData_Then_ReturnsFromCache()
    {
        // Arrange
        var cachedData = new List<CatalogTypeDto>
        {
            new() { Id = 1, Type = "CachedType" }
        };
        _memoryCache.Set("CatalogTypes", cachedData);

        var handler = new GetCatalogTypes.Query.Handler(_dbContext, _memoryCache);
        var query = new GetCatalogTypes.Query();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Data.Count.ShouldBe(1);
        result.Data[0].Type.ShouldBe("CachedType");
    }
}
