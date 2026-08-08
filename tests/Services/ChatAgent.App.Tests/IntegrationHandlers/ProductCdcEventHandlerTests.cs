using ChatAgent.App.Cdc;
using ChatAgent.App.Data;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Data.Enums;
using ChatAgent.App.IntegrationHandlers;
using ChatAgent.App.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ChatAgent.App.Tests.IntegrationHandlers;

public class ProductCdcEventHandlerTests
{
    private static TestChatAgentDbContext CreateDbContext() => TestChatAgentDbContext.Create();

    private static ProductCdcEventHandler CreateHandler(ChatAgentDbContext dbContext)
        => new(dbContext, new VectorIndexer(dbContext), NullLogger<ProductCdcEventHandler>.Instance);

    private static ProductCdcEvent CreateEvent(Guid id, string name = "Pho Bo", decimal price = 12.5m) => new()
    {
        ProductId = id,
        Name = name,
        Description = "Beef noodle soup",
        CategoryName = "Soups",
        ImageUrl = "https://example.com/pho.jpg",
        Price = price,
        CatalogTypeId = 3,
    };

    [Fact]
    public async Task HandleAsync_When_ProductIsNew_Then_CreatesMirrorRowAndIndexEntry()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();

        await CreateHandler(dbContext).HandleAsync(CreateEvent(productId));

        var product = await dbContext.Products.SingleAsync();
        product.Id.ShouldBe(productId);
        product.Name.ShouldBe("Pho Bo");
        product.Price.ShouldBe(12.5m);

        var indexed = await dbContext.VectorDocuments.SingleAsync();
        indexed.SourceType.ShouldBe(VectorSourceType.Product);
        indexed.SourceId.ShouldBe(productId.ToString());
        indexed.Content.ShouldContain("Pho Bo");
        indexed.Content.ShouldContain("Soups");

        // Embedding is deferred to the backfill worker so CDC never blocks on Azure OpenAI.
        indexed.EmbeddedAt.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_When_ProductExists_Then_UpdatesMirrorRowInPlace()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(CreateEvent(productId));
        await handler.HandleAsync(CreateEvent(productId, name: "Pho Ga", price: 11m));

        var product = await dbContext.Products.SingleAsync();
        product.Name.ShouldBe("Pho Ga");
        product.Price.ShouldBe(11m);

        dbContext.VectorDocuments.Count().ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_When_ContentChanges_Then_InvalidatesTheEmbedding()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(CreateEvent(productId));

        // Simulate the backfill worker having embedded the row.
        var indexed = await dbContext.VectorDocuments.SingleAsync();
        indexed.EmbeddedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        await handler.HandleAsync(CreateEvent(productId, name: "Bun Bo Hue"));

        var refreshed = await dbContext.VectorDocuments.SingleAsync();
        refreshed.EmbeddedAt.ShouldBeNull();
        refreshed.Content.ShouldContain("Bun Bo Hue");
    }

    [Fact]
    public async Task HandleAsync_When_ReplayHasIdenticalContent_Then_KeepsTheExistingEmbedding()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(CreateEvent(productId));

        var embeddedAt = DateTime.UtcNow;
        var indexed = await dbContext.VectorDocuments.SingleAsync();
        indexed.EmbeddedAt = embeddedAt;
        await dbContext.SaveChangesAsync();

        // CDC is at-least-once; a redelivery must not trigger pointless re-embedding.
        await handler.HandleAsync(CreateEvent(productId));

        var refreshed = await dbContext.VectorDocuments.SingleAsync();
        refreshed.EmbeddedAt.ShouldBe(embeddedAt);
    }

    [Fact]
    public async Task HandleAsync_When_EventIsDeleted_Then_RemovesMirrorRowAndIndexEntry()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(CreateEvent(productId));

        var deleteEvent = CreateEvent(productId);
        deleteEvent.DeletedRaw = "true";
        await handler.HandleAsync(deleteEvent);

        dbContext.Products.ShouldBeEmpty();
        dbContext.VectorDocuments.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_When_DeletingUnknownProduct_Then_DoesNothing()
    {
        await using var dbContext = CreateDbContext();

        var deleteEvent = CreateEvent(Guid.NewGuid());
        deleteEvent.DeletedRaw = "true";

        await Should.NotThrowAsync(() => CreateHandler(dbContext).HandleAsync(deleteEvent));

        dbContext.Products.ShouldBeEmpty();
    }
}

public class CatalogTypeCdcEventHandlerTests
{
    private static TestChatAgentDbContext CreateDbContext() => TestChatAgentDbContext.Create();

    private static CatalogTypeCdcEventHandler CreateHandler(ChatAgentDbContext dbContext)
        => new(dbContext, new VectorIndexer(dbContext), NullLogger<CatalogTypeCdcEventHandler>.Instance);

    [Fact]
    public async Task HandleAsync_When_CategoryIsNew_Then_CreatesMirrorRowAndIndexEntry()
    {
        await using var dbContext = CreateDbContext();

        await CreateHandler(dbContext).HandleAsync(new CatalogTypeCdcEvent
        {
            CatalogTypeId = 7,
            Type = "Desserts",
        });

        var category = await dbContext.ProductCategories.SingleAsync();
        category.Id.ShouldBe(7);
        category.Name.ShouldBe("Desserts");

        var indexed = await dbContext.VectorDocuments.SingleAsync();
        indexed.SourceType.ShouldBe(VectorSourceType.ProductCategory);
        indexed.SourceId.ShouldBe("7");
        indexed.Content.ShouldContain("Desserts");
    }

    [Fact]
    public async Task HandleAsync_When_CategoryIsRenamed_Then_UpdatesMirrorRow()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(new CatalogTypeCdcEvent { CatalogTypeId = 7, Type = "Desserts" });
        await handler.HandleAsync(new CatalogTypeCdcEvent { CatalogTypeId = 7, Type = "Sweets" });

        var category = await dbContext.ProductCategories.SingleAsync();
        category.Name.ShouldBe("Sweets");
        dbContext.ProductCategories.Count().ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_When_EventIsDeleted_Then_RemovesMirrorRowAndIndexEntry()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(new CatalogTypeCdcEvent { CatalogTypeId = 7, Type = "Desserts" });
        await handler.HandleAsync(new CatalogTypeCdcEvent { CatalogTypeId = 7, Type = "Desserts", DeletedRaw = "true" });

        dbContext.ProductCategories.ShouldBeEmpty();
        dbContext.VectorDocuments.ShouldBeEmpty();
    }
}
