namespace Products.API.Dtos;

public record CatalogTypeDto
{
    public int Id { get; set; }
    public required string Type { get; set; }
}
