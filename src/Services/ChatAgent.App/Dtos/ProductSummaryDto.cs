namespace ChatAgent.App.Dtos;

/// <summary>
/// What the agent is allowed to see about a product. Projected from the locally
/// replicated read-model.
/// </summary>
public record ProductSummaryDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string CategoryName { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Units available, or null when no CDC record has carried a stock value for this product yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Null means unknown, not zero, and the agent's grounding rules say so explicitly - reporting
    /// an unknown as "out of stock" is a false claim, and the failure mode is the whole menu.
    /// </para>
    /// <para>
    /// Exposed as a raw number rather than an in-stock/out-of-stock enum on purpose. The response
    /// guard verifies the draft against serialised tool output, so a number the model can quote is
    /// a number the fact checker can verify; an enum would make "we have three left" plausible and
    /// uncheckable. The staleness that comes with a replicated value is handled in the prompt.
    /// </para>
    /// </remarks>
    public int? AvailableStock { get; init; }
}
