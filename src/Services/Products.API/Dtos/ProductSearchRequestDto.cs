using System.ComponentModel;

namespace Products.API.Dtos;

public record ProductSearchRequestDto
{
    [DefaultValue(1)]
    public int? PageIndex { get; set; }
    [DefaultValue(10)]
    public int? PageSize { get; set; }
    public int? CatalogTypeId { get; set; }
    public string? Search { get; set; }
}
