using ChatAgent.App.Data;
using ChatAgent.App.Guards.Grounding;
using Mango.Core.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatAgent.App.Guards.Authorization;

/// <summary>
/// Decides whether a state-changing tool call may proceed.
/// </summary>
public interface IToolAuthorizer
{
    Task<ToolAuthorizationDecision> AuthorizeAsync(
        string functionName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IToolAuthorizer"/>
/// <remarks>
/// <para>
/// All the rule logic lives here rather than in the Semantic Kernel filter, because a
/// <c>FunctionInvocationContext</c> cannot be constructed by a test - putting the rules behind
/// the filter would make them reachable only through a running kernel.
/// </para>
/// <para>
/// The arguments this inspects were chosen by the model, and the model chose them after reading
/// product descriptions, store documents and web results. They are untrusted input in the same
/// sense a request body is, and are validated the same way.
/// </para>
/// </remarks>
public class ToolAuthorizer : IToolAuthorizer
{
    private readonly ChatAgentDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IGroundingContext _grounding;
    private readonly ToolWriteBudget _budget;
    private readonly ToolAuthorizationOptions _options;
    private readonly ILogger<ToolAuthorizer> _logger;

    public ToolAuthorizer(
        ChatAgentDbContext dbContext,
        ICurrentUserContext currentUser,
        IGroundingContext grounding,
        ToolWriteBudget budget,
        IOptions<AIAgentConfiguration> options,
        ILogger<ToolAuthorizer> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _grounding = grounding;
        _budget = budget;
        _options = options.Value.ToolAuthorization;
        _logger = logger;
    }

    public async Task<ToolAuthorizationDecision> AuthorizeAsync(
        string functionName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        // Reads are the overwhelming majority of calls and change nothing, so they cost no
        // database round-trip and no rule evaluation.
        if (!ToolCatalog.IsWrite(functionName))
        {
            return ToolAuthorizationDecision.Allow;
        }

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return ToolAuthorizationDecision.Deny(
                "not-authenticated", "I need you to be signed in before I can change your order.");
        }

        // No tool takes an identity argument today, and this is what keeps it that way: the day
        // one does, it fails closed rather than trusting whatever the model passed.
        foreach (var key in IdentityArgumentNames)
        {
            if (arguments.TryGetValue(key, out var raw) && raw is not null)
            {
                var supplied = raw.ToString();
                if (!string.Equals(supplied, _currentUser.UserId, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "Tool {Tool} was called with {Argument} that is not the signed-in customer.",
                        functionName, key);

                    return ToolAuthorizationDecision.Deny(
                        "resource-ownership", "I can only make changes to your own order.");
                }
            }
        }

        if (!_options.EnabledWriteTools.Contains(functionName, StringComparer.OrdinalIgnoreCase))
        {
            return ToolAuthorizationDecision.Deny(
                "tool-disabled", "That isn't something I can do right now.");
        }

        if (!_budget.TryConsume(_options.MaxWritesPerTurn))
        {
            _logger.LogWarning(
                "Tool {Tool} denied: the turn's write budget of {Limit} is spent.",
                functionName, _options.MaxWritesPerTurn);

            return ToolAuthorizationDecision.Deny(
                "write-budget", "I've already made a few changes — could you confirm what you'd like next?");
        }

        return functionName switch
        {
            "AddProductAsync" => await AuthorizeAddProductAsync(arguments, cancellationToken),
            "ApplyCouponAsync" => AuthorizeApplyCoupon(arguments),
            _ => ToolAuthorizationDecision.Allow,
        };
    }

    private async Task<ToolAuthorizationDecision> AuthorizeAddProductAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        if (!TryGetGuid(arguments, "productId", out var productId))
        {
            return ToolAuthorizationDecision.Deny(
                "invalid-argument", "I couldn't tell which dish you meant — could you name it?");
        }

        if (!TryGetInt(arguments, "quantity", out var quantity)
            || quantity < 1
            || quantity > _options.MaxQuantityPerAdd)
        {
            return ToolAuthorizationDecision.Deny(
                "invalid-argument",
                $"I can add between 1 and {_options.MaxQuantityPerAdd} of an item at a time.");
        }

        // The replicated catalogue, not the vector index: the index holds the text a search
        // matched on, which may be stale or flagged, while this row is what the price comes from.
        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.Name, p.Price, p.AvailableStock })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return ToolAuthorizationDecision.Deny(
                "product-not-found", "I couldn't find that dish on our menu any more.");
        }

        if (product.Price <= 0)
        {
            // Almost certainly a replication fault rather than an attack, but the outcome of
            // proceeding is a free order either way.
            _logger.LogError(
                "Refusing to add product {ProductId}: replicated price is {Price}.", product.Id, product.Price);

            return ToolAuthorizationDecision.Deny(
                "price-invalid", "There's a problem with that item's price — I can't add it right now.");
        }

        if (arguments.TryGetValue("price", out var suppliedPrice)
            && suppliedPrice is not null
            && decimal.TryParse(suppliedPrice.ToString(), out var claimed)
            && claimed != product.Price)
        {
            _logger.LogWarning(
                "Refusing to add product {ProductId}: supplied price {Claimed} does not match {Actual}.",
                product.Id, claimed, product.Price);

            return ToolAuthorizationDecision.Deny(
                "price-invalid", "That price doesn't match our menu — let me check it for you.");
        }

        if (_options.RequireStock && product.AvailableStock is int stock && quantity > stock)
        {
            return ToolAuthorizationDecision.Deny(
                "insufficient-stock",
                stock <= 0
                    ? "That dish is unavailable at the moment."
                    : $"I can only add {stock} of those right now.");
        }

        // Recording the authoritative row means the answer's price can be checked against what
        // this service holds, not merely against whatever a search tool happened to return.
        _grounding.Record(
            "ToolAuthorization.ProductSnapshot",
            JsonSerializer.Serialize(new { product.Id, product.Name, product.Price, Quantity = quantity }));

        return ToolAuthorizationDecision.Allow;
    }

    private ToolAuthorizationDecision AuthorizeApplyCoupon(IReadOnlyDictionary<string, object?> arguments)
    {
        var code = arguments.TryGetValue("code", out var raw) ? raw?.ToString() : null;

        if (string.IsNullOrWhiteSpace(code)
            || !Regex.IsMatch(code, _options.CouponCodePattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
        {
            return ToolAuthorizationDecision.Deny(
                "invalid-argument", "That doesn't look like one of our coupon codes.");
        }

        return ToolAuthorizationDecision.Allow;
    }

    private static bool TryGetGuid(IReadOnlyDictionary<string, object?> arguments, string key, out Guid value)
    {
        value = Guid.Empty;

        if (!arguments.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is Guid guid)
        {
            value = guid;
            return guid != Guid.Empty;
        }

        return Guid.TryParse(raw.ToString(), out value) && value != Guid.Empty;
    }

    private static bool TryGetInt(IReadOnlyDictionary<string, object?> arguments, string key, out int value)
    {
        value = 0;

        if (!arguments.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is int number)
        {
            value = number;
            return true;
        }

        return int.TryParse(raw.ToString(), out value);
    }

    private static readonly string[] IdentityArgumentNames = ["userId", "customerId", "cartId"];
}
