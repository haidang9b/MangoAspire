using ChatAgent.App.Cdc;
using Shouldly;
using System.Text.Json;

namespace ChatAgent.App.Tests.Cdc;

public class DebeziumNumericConverterTests
{
    private record Wrapper
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(DebeziumNumericConverter))]
        public decimal Value { get; set; }
    }

    private static decimal Deserialize(string json)
        => JsonSerializer.Deserialize<Wrapper>(json)!.Value;

    [Fact]
    public void Read_When_ValueIsDebeziumDecimalObject_Then_AppliesScale()
    {
        // 1799 with scale 2 == 17.99. 1799 is 0x0707 as a big-endian integer.
        var encoded = Convert.ToBase64String([0x07, 0x07]);

        var result = Deserialize($$$"""{"Value":{"scale":2,"value":"{{{encoded}}}"}}""");

        result.ShouldBe(17.99m);
    }

    [Fact]
    public void Read_When_ValueIsNegative_Then_DecodesTwosComplement()
    {
        // 0xFF 0xFF == -1 as a big-endian two's-complement integer, scale 2 == -0.01.
        var encoded = Convert.ToBase64String([0xFF, 0xFF]);

        var result = Deserialize($$$"""{"Value":{"scale":2,"value":"{{{encoded}}}"}}""");

        result.ShouldBe(-0.01m);
    }

    [Fact]
    public void Read_When_ValueIsPlainNumber_Then_ReturnsIt()
    {
        var result = Deserialize("""{"Value":42.5}""");

        result.ShouldBe(42.5m);
    }

    [Fact]
    public void Read_When_ValueIsString_Then_ParsesInvariant()
    {
        var result = Deserialize("""{"Value":"12.75"}""");

        result.ShouldBe(12.75m);
    }

    [Fact]
    public void Read_When_ValueObjectHasNoBytes_Then_ReturnsZero()
    {
        var result = Deserialize("""{"Value":{"scale":2,"value":""}}""");

        result.ShouldBe(0m);
    }
}

public class ProductCdcEventTests
{
    [Fact]
    public void IsDeleted_When_DeletedFlagIsTrueString_Then_ReturnsTrue()
    {
        // Debezium's delete.handling.mode=rewrite emits the flag as a string, not a bool.
        var cdcEvent = new ProductCdcEvent { DeletedRaw = "true" };

        cdcEvent.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void IsDeleted_When_DeletedFlagIsFalseOrMissing_Then_ReturnsFalse()
    {
        new ProductCdcEvent { DeletedRaw = "false" }.IsDeleted.ShouldBeFalse();
        new ProductCdcEvent { DeletedRaw = null }.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Deserialize_When_PayloadUsesSnakeCaseColumns_Then_MapsToProperties()
    {
        var json = """
            {
              "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "name": "Pho Bo",
              "price": 12.5,
              "description": "Beef noodle soup",
              "category_name": "Soups",
              "image_url": "https://example.com/pho.jpg",
              "catalog_type_id": 3,
              "__deleted": "false"
            }
            """;

        var result = JsonSerializer.Deserialize<ProductCdcEvent>(json)!;

        result.ProductId.ShouldBe(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
        result.Name.ShouldBe("Pho Bo");
        result.Price.ShouldBe(12.5m);
        result.CategoryName.ShouldBe("Soups");
        result.ImageUrl.ShouldBe("https://example.com/pho.jpg");
        result.CatalogTypeId.ShouldBe(3);
        result.IsDeleted.ShouldBeFalse();
    }
}

public class CatalogTypeCdcEventTests
{
    [Fact]
    public void Deserialize_When_PayloadUsesUpstreamColumnNames_Then_MapsTypeToName()
    {
        var json = """{"id":7,"type":"Desserts","__deleted":"false"}""";

        var result = JsonSerializer.Deserialize<CatalogTypeCdcEvent>(json)!;

        result.CatalogTypeId.ShouldBe(7);
        result.Type.ShouldBe("Desserts");
        result.IsDeleted.ShouldBeFalse();
    }
}
