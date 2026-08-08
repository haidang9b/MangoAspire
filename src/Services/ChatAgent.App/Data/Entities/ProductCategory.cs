namespace ChatAgent.App.Data.Entities;

/// <summary>
/// Local read-model of a Products.API <c>catalog_types</c> row, kept in sync by
/// Debezium CDC (<see cref="Cdc.CatalogTypeCdcEvent"/>). Never written to by this service.
/// </summary>
public class ProductCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>When this row was last written from a CDC message.</summary>
    public DateTime UpdatedAt { get; set; }
}
