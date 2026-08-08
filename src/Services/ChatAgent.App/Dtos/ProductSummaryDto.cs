namespace ChatAgent.App.Dtos;

/// <summary>
/// What the agent is allowed to see about a product. Projected from the locally
/// replicated read-model.
/// </summary>
/// <remarks>
/// Deliberately carries no stock field: the CDC stream excludes <c>available_stock</c>, so
/// exposing one would invite the model to make availability claims it cannot support.
/// </remarks>
public record ProductSummaryDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string CategoryName { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
}
