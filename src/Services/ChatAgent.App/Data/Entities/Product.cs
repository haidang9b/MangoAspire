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

    /// <summary>When this row was last written from a CDC message — a local processing time.</summary>
    /// <remarks>Wall clock at write time, so it must never be used to order CDC events; that is
    /// what <see cref="SourceLsn"/> is for.</remarks>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// WAL position of the CDC record this row reflects. The replay fence: an incoming event
    /// whose LSN is not greater than this is discarded rather than applied.
    /// </summary>
    public long? SourceLsn { get; set; }

    /// <summary>Commit time of the CDC record this row reflects; the fence's fallback.</summary>
    public DateTime? SourceTimestamp { get; set; }

    /// <summary>
    /// Tombstone for an upstream delete. The row is kept rather than removed so that its
    /// <see cref="SourceLsn"/> survives — otherwise a replayed older insert would silently
    /// resurrect a deleted product. A global query filter hides these from every read path.
    /// </summary>
    public bool IsDeleted { get; set; }
}
