using EventBus.Abstractions;
using System.Text.Json.Serialization;

namespace ChatAgent.App.Cdc;

/// <summary>
/// Debezium change record for <c>productdb.public.catalog_types</c> — the product
/// categories — read off the <c>mango.cdc.stream</c> log.
/// </summary>
[EventName("mango.public.catalog_types")]
public record CatalogTypeCdcEvent : CdcIntegrationEvent
{
    [JsonPropertyName("id")]
    public int CatalogTypeId { get; set; }

    /// <summary>Upstream column is <c>type</c>; surfaced locally as the category name.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
}
