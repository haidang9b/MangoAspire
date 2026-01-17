using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Products.API.Data.Migrations;

/// <inheritdoc />
public partial class SeedData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "products",
            columns: new[] { "id", "category_name", "description", "image_url", "name", "price" },
            values: new object[,]
            {
                { new Guid("348cc1a0-707b-4e1b-9700-de40d54acd31"), "Appetizer", "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium.", "https://phanhaidang.blob.core.windows.net/mango/12.jpg", "Paneer Tikka", 13.99m },
                { new Guid("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"), "Entree", "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium.", "https://phanhaidang.blob.core.windows.net/mango/13.jpg", "Pav Bhaji", 15m },
                { new Guid("970afb7e-5616-46c4-b3fa-f9abb62928db"), "Appetizer", "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium.", "https://phanhaidang.blob.core.windows.net/mango/14.jpg", "Samosa", 15m },
                { new Guid("bc047267-5eb9-4681-b489-4473ccd00a87"), "Dessert", "Praesent scelerisque, mi sed ultrices condimentum, lacus ipsum viverra massa, in lobortis sapien eros in arcu. Quisque vel lacus ac magna vehicula sagittis ut non lacus.<br/>Sed volutpat tellus lorem, lacinia tincidunt tellus varius nec. Vestibulum arcu turpis, facilisis sed ligula ac, maximus malesuada neque. Phasellus commodo cursus pretium.", "https://phanhaidang.blob.core.windows.net/mango/11.jpg", "Sweet Pie", 10.99m }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "products",
            keyColumn: "id",
            keyValue: new Guid("348cc1a0-707b-4e1b-9700-de40d54acd31"));

        migrationBuilder.DeleteData(
            table: "products",
            keyColumn: "id",
            keyValue: new Guid("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"));

        migrationBuilder.DeleteData(
            table: "products",
            keyColumn: "id",
            keyValue: new Guid("970afb7e-5616-46c4-b3fa-f9abb62928db"));

        migrationBuilder.DeleteData(
            table: "products",
            keyColumn: "id",
            keyValue: new Guid("bc047267-5eb9-4681-b489-4473ccd00a87"));
    }
}
