namespace ChatAgent.App.Data.Entities;

/// <summary>
/// Local read-model of a Products.API product, kept in sync by Debezium CDC
/// (<see cref="Cdc.ProductCdcEvent"/>). Never written to by this service.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CategoryName { get; set; }
    public required string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int? CatalogTypeId { get; set; }

    /// <summary>
    /// Units available upstream at the LSN this row reflects.
    /// </summary>
    /// <remarks>
    /// Null means "no CDC record has carried a stock value yet" — history published before the
    /// column joined the capture list, or a row not re-snapshotted since. That is not the same as
    /// zero, and the difference matters: reporting an unknown as "out of stock" is a false claim
    /// about every dish on the menu.
    /// </remarks>
    public int? AvailableStock { get; set; }

    /// <summary>
    /// Set when the replicated name or description tripped the injection scanner.
    /// </summary>
    /// <remarks>
    /// Product text is authored in another service and is therefore untrusted. The row still
    /// replicates when this is set — upstream text must never decide whether replication happens,
    /// or an attacker could hide a product by poisoning it — but read paths omit the description
    /// so the suspect text is not handed to the model as menu copy.
    /// </remarks>
    public bool ContentFlagged { get; set; }

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
