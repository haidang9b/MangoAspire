using EventBus.Abstractions;
using EventBus.Events;
using System.Text.Json.Serialization;

namespace ChatAgent.App.Cdc;

/// <summary>
/// Debezium change record for <c>productdb.public.catalog_types</c> — the product
/// categories — delivered on the <c>mango-cdc-exchange</c> direct exchange.
/// </summary>
[EventName("mango.public.catalog_types")]
public record CatalogTypeCdcEvent : IntegrationEvent
{
    [JsonPropertyName("id")]
    public int CatalogTypeId { get; set; }

    /// <summary>Upstream column is <c>type</c>; surfaced locally as the category name.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    /// <inheritdoc cref="ProductCdcEvent.DeletedRaw"/>
    [JsonPropertyName("__deleted")]
    public string? DeletedRaw { get; set; }

    [JsonIgnore]
    public bool IsDeleted => string.Equals(DeletedRaw, "true", StringComparison.OrdinalIgnoreCase);
}
