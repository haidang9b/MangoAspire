using ChatAgent.App.Configurations;
using ChatAgent.App.Data.Entities;
using ChatAgent.App.Guards.Authorization;
using ChatAgent.App.Guards.Grounding;
using Mango.Core.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace ChatAgent.App.Tests.Guards.Authorization;

public class ToolAuthorizerTests
{
    private const string UserId = "customer-1";

    private readonly TestChatAgentDbContext _dbContext = TestChatAgentDbContext.Create();
    private readonly GroundingContext _grounding = new();

    private ToolAuthorizer CreateAuthorizer(
        ToolAuthorizationOptions? options = null,
        ICurrentUserContext? currentUser = null)
        => new(
            _dbContext,
            currentUser ?? SignedIn(UserId),
            _grounding,
            new ToolWriteBudget(),
            Options.Create(new AIAgentConfiguration
            {
                ToolAuthorization = options ?? new ToolAuthorizationOptions(),
            }),
            NullLogger<ToolAuthorizer>.Instance);

    private static ICurrentUserContext SignedIn(string userId)
        => Mock.Of<ICurrentUserContext>(u => u.UserId == userId && u.IsAuthenticated == true);

    private async Task<Guid> SeedProductAsync(decimal price = 12.50m, int? stock = 10, bool deleted = false)
    {
        var id = Guid.NewGuid();

        _dbContext.Products.Add(new Product
        {
            Id = id,
            Name = "Pho Bo",
            Description = "Beef noodle soup",
            CategoryName = "Noodles",
            ImageUrl = "http://localhost/pho.png",
            Price = price,
            AvailableStock = stock,
            IsDeleted = deleted,
            UpdatedAt = DateTime.UtcNow,
        });

        await _dbContext.SaveChangesAsync();
        return id;
    }

    private static Dictionary<string, object?> AddArgs(Guid productId, int quantity)
        => new(StringComparer.OrdinalIgnoreCase) { ["productId"] = productId, ["quantity"] = quantity };

    [Fact]
    public async Task AuthorizeAsync_When_FunctionIsRead_Then_AllowsImmediately()
    {
        var decision = await CreateAuthorizer().AuthorizeAsync("SearchProductsAsync", new Dictionary<string, object?>());

        decision.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_When_UserIsNotAuthenticated_Then_DeniesWrite()
    {
        var anonymous = Mock.Of<ICurrentUserContext>(u => u.IsAuthenticated == false);
        var productId = await SeedProductAsync();

        var decision = await CreateAuthorizer(currentUser: anonymous)
            .AuthorizeAsync("AddProductAsync", AddArgs(productId, 1));

        decision.Allowed.ShouldBeFalse();
        decision.RuleId.ShouldBe("not-authenticated");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ArgumentsNameAnotherCustomer_Then_DeniesWrite()
    {
        // No tool takes an identity argument today. This is the tripwire for the day one does.
        var productId = await SeedProductAsync();
        var arguments = AddArgs(productId, 1);
        arguments["userId"] = "someone-else";

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", arguments);

        decision.Allowed.ShouldBeFalse();
        decision.RuleId.ShouldBe("resource-ownership");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ToolIsNotInTheAllowList_Then_Denies()
    {
        var productId = await SeedProductAsync();
        var options = new ToolAuthorizationOptions { EnabledWriteTools = ["ApplyCouponAsync"] };

        var decision = await CreateAuthorizer(options).AuthorizeAsync("AddProductAsync", AddArgs(productId, 1));

        decision.RuleId.ShouldBe("tool-disabled");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ProductDoesNotExist_Then_DeniesAddProduct()
    {
        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(Guid.NewGuid(), 1));

        decision.RuleId.ShouldBe("product-not-found");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ProductIsTombstoned_Then_DeniesAddProduct()
    {
        // Upstream deletes tombstone rather than remove, so the global query filter is what hides
        // a delisted dish. If that filter were ever dropped, this test is what notices.
        var productId = await SeedProductAsync(deleted: true);

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, 1));

        decision.RuleId.ShouldBe("product-not-found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(9999)]
    public async Task AuthorizeAsync_When_QuantityIsOutOfRange_Then_DeniesAddProduct(int quantity)
    {
        var productId = await SeedProductAsync();

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, quantity));

        decision.RuleId.ShouldBe("invalid-argument");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ProductIdIsNotAGuid_Then_DeniesAddProduct()
    {
        var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["productId"] = "the tasty one",
            ["quantity"] = 1,
        };

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", arguments);

        decision.RuleId.ShouldBe("invalid-argument");
    }

    [Fact]
    public async Task AuthorizeAsync_When_ReplicatedPriceIsNotPositive_Then_DeniesAddProduct()
    {
        // A replication fault rather than an attack, but proceeding means a free order either way.
        var productId = await SeedProductAsync(price: 0m);

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, 1));

        decision.RuleId.ShouldBe("price-invalid");
    }

    [Fact]
    public async Task AuthorizeAsync_When_SuppliedPriceDoesNotMatchTheCatalogue_Then_DeniesAddProduct()
    {
        var productId = await SeedProductAsync(price: 12.50m);
        var arguments = AddArgs(productId, 1);
        arguments["price"] = 0.01m;

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", arguments);

        decision.RuleId.ShouldBe("price-invalid");
    }

    [Fact]
    public async Task AuthorizeAsync_When_StockIsLowerThanRequested_Then_DeniesAddProduct()
    {
        var productId = await SeedProductAsync(stock: 2);

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, 5));

        decision.RuleId.ShouldBe("insufficient-stock");
    }

    [Fact]
    public async Task AuthorizeAsync_When_StockIsUnknown_Then_AllowsAddProduct()
    {
        // Null means "never replicated", not "none left". Refusing here would block the whole
        // menu on any product CDC has not carried a stock value for yet.
        var productId = await SeedProductAsync(stock: null);

        var decision = await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, 5));

        decision.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_When_WriteIsAllowed_Then_RecordsTheAuthoritativeProductInGrounding()
    {
        var productId = await SeedProductAsync(price: 12.50m);

        await CreateAuthorizer().AuthorizeAsync("AddProductAsync", AddArgs(productId, 2));

        var snapshot = _grounding.Snapshot();
        snapshot.Entries.ShouldContain(e => e.ToolName == "ToolAuthorization.ProductSnapshot");
        snapshot.Entries.First(e => e.ToolName == "ToolAuthorization.ProductSnapshot")
            .Result.ShouldContain("12.5");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no spaces allowed")]
    [InlineData("ab")]
    public async Task AuthorizeAsync_When_CouponCodeIsMalformed_Then_DeniesApplyCoupon(string code)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["code"] = code };

        var decision = await CreateAuthorizer().AuthorizeAsync("ApplyCouponAsync", arguments);

        decision.RuleId.ShouldBe("invalid-argument");
    }

    [Fact]
    public async Task AuthorizeAsync_When_WriteBudgetIsExhausted_Then_Denies()
    {
        var productId = await SeedProductAsync();
        var authorizer = CreateAuthorizer(new ToolAuthorizationOptions { MaxWritesPerTurn = 1 });

        (await authorizer.AuthorizeAsync("AddProductAsync", AddArgs(productId, 1))).Allowed.ShouldBeTrue();

        var second = await authorizer.AuthorizeAsync("AddProductAsync", AddArgs(productId, 1));
        second.RuleId.ShouldBe("write-budget");
    }
}
