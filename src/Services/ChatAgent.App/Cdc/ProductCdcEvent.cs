using EventBus.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAgent.App.Cdc;

/// <summary>
/// Debezium change record for <c>productdb.public.products</c>, delivered on the
/// <c>mango-cdc-exchange</c> direct exchange. Property names mirror the physical Postgres
/// columns, not the upstream CLR entity, because that is what Debezium emits.
/// </summary>
/// <remarks>
/// ShoppingCart.API consumes the same routing key off the shared <c>mango.cdc.stream</c> log,
/// reading it independently from its own offset, so the two read-models never interfere.
/// <c>available_stock</c> is excluded upstream via <c>column.exclude.list</c>.
/// </remarks>
[EventName("mango.public.products")]
public record ProductCdcEvent : CdcIntegrationEvent
{
    [JsonPropertyName("id")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("price")]
    [JsonConverter(typeof(DebeziumNumericConverter))]
    public decimal Price { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = default!;

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = default!;

    [JsonPropertyName("catalog_type_id")]
    public int? CatalogTypeId { get; set; }
}

/// <summary>
/// Converts Debezium's decimal wire format <c>{"scale":2,"value":"Bwc="}</c> — a base64
/// big-endian two's-complement integer plus a scale — into <see cref="decimal"/>.
/// Plain JSON numbers and strings are accepted too, since the representation depends on
/// the connector's <c>decimal.handling.mode</c>.
/// </summary>
public class DebeziumNumericConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return decimal.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int scale = 0;
            byte[]? valueBytes = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();

                if (propName == "scale")
                {
                    scale = reader.GetInt32();
                }
                else if (propName == "value")
                {
                    var base64 = reader.GetString();
                    if (!string.IsNullOrEmpty(base64))
                    {
                        valueBytes = Convert.FromBase64String(base64);
                    }
                }
            }

            if (valueBytes is { Length: > 0 })
            {
                var bigInt = new System.Numerics.BigInteger(valueBytes, isUnsigned: false, isBigEndian: true);
                var divisor = (decimal)Math.Pow(10, scale);
                return (decimal)bigInt / divisor;
            }

            return 0m;
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to decimal");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
