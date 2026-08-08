namespace ShoppingCart.API.Entities;

/// <summary>
/// Local read-model of a Products.API product, kept in sync from the replayable CDC log
/// (<see cref="Cdc.ProductCdcEvent"/>). Never written to by this service.
/// </summary>
public class Product : EntityBase<Guid>
{
    public required string Name { get; set; }

    public decimal Price { get; set; }

    public required string Description { get; set; }

    public required string CategoryName { get; set; }

    public required string ImageUrl { get; set; }

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
    /// resurrect a deleted product.
    /// </summary>
    /// <remarks>
    /// Unlike ChatAgent's mirror there is no global query filter here: <c>CartDetails.Product</c>
    /// is a required navigation and filtering it would break the cart projection in
    /// <c>GetCartHandler</c>. Carts keep rendering a delisted product they already contain;
    /// what changes is that <c>UpsertCart</c> refuses to add one.
    /// </remarks>
    public bool IsDeleted { get; set; }
}
