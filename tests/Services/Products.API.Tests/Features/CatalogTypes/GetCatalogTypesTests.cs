using Mango.Core.Caching;
using Mango.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Features.CatalogTypes;

namespace Products.API.Tests.Features.CatalogTypes;

public class GetCatalogTypesTests
{
    private readonly ProductDbContext _dbContext;
    private readonly ICacheManager _cacheManager;

    public GetCatalogTypesTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ProductDbContext(options);

        // A real HybridCache instance: the cache behaviour under test (miss
        // populates from the database, hit bypasses it) is the point of the
        // test, so mocking it away would leave nothing meaningful to assert.
        var hybridCache = new ServiceCollection()
            .AddHybridCache()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<HybridCache>();

        _cacheManager = new HybridCacheManager(hybridCache);
    }

    [Fact]
    public async Task HandleAsync_When_CacheIsEmpty_Then_ReturnsFromDbAndSetsCache()
    {
        // Arrange
        var catalogType = new CatalogType { Id = 1, Type = "Type1" };
        _dbContext.CatalogTypes.Add(catalogType);
        await _dbContext.SaveChangesAsync();

        var handler = new GetCatalogTypes.Query.Handler(_dbContext, _cacheManager);
        var query = new GetCatalogTypes.Query();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Data.Count.ShouldBe(1);
        result.Data[0].Type.ShouldBe("Type1");

        // The value was cached, so a second call must not observe a later
        // database change.
        _dbContext.CatalogTypes.Add(new CatalogType { Id = 2, Type = "Type2" });
        await _dbContext.SaveChangesAsync();

        var cachedResult = await handler.HandleAsync(query, CancellationToken.None);
        cachedResult.Data.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_When_CacheHasData_Then_ReturnsFromCache()
    {
        // Arrange
        var cachedData = new List<CatalogTypeDto>
        {
            new() { Id = 1, Type = "CachedType" }
        };
        await _cacheManager.SetAsync("CatalogTypes", cachedData);

        var handler = new GetCatalogTypes.Query.Handler(_dbContext, _cacheManager);
        var query = new GetCatalogTypes.Query();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Data.Count.ShouldBe(1);
        result.Data[0].Type.ShouldBe("CachedType");
    }
}
