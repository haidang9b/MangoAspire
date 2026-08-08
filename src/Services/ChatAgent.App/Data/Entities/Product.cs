namespace ChatAgent.App.Data.Entities;

/// <summary>
/// Local read-model of a Products.API product, kept in sync by Debezium CDC
/// (<see cref="Cdc.ProductCdcEvent"/>). Never written to by this service.
/// </summary>
/// <remarks>
/// <c>available_stock</c> is deliberately absent: the CDC stream excludes it so that
/// saga-driven stock churn does not fan out to every subscriber. The agent must not
/// make stock claims.
/// </remarks>
public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CategoryName { get; set; }
    public required string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int? CatalogTypeId { get; set; }

    /// <summary>When this row was last written from a CDC message.</summary>
    public DateTime UpdatedAt { get; set; }
}
