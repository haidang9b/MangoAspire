using EventBus.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShoppingCart.API.Cdc;

[EventName("mango.public.products")]
public record ProductCdcEvent : IntegrationEvent
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

    [JsonPropertyName("__deleted")]
    public string? DeletedRaw { get; set; }

    [JsonIgnore]
    public bool IsDeleted => DeletedRaw?.ToLower() == "true";
}

/// <summary>
/// Converts Debezium numeric format {"scale":2,"value":"Bwc="} to decimal.
/// The value is a base64-encoded big-endian integer.
/// </summary>
public class DebeziumNumericConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle simple number values
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        // Handle string values
        if (reader.TokenType == JsonTokenType.String)
        {
            return decimal.Parse(reader.GetString()!);
        }

        // Handle Debezium numeric object {"scale": N, "value": "base64"}
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int scale = 0;
            byte[]? valueBytes = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
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
            }

            if (valueBytes != null && valueBytes.Length > 0)
            {
                // Convert big-endian bytes to BigInteger, then to decimal with scale
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
