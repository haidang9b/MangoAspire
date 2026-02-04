using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Products.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsForCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "available_stock",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "catalog_brand_id",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "catalog_type_id",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_brands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    brand = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_types", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("348cc1a0-707b-4e1b-9700-de40d54acd31"),
                columns: new[] { "available_stock", "catalog_brand_id", "catalog_type_id" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("7e51b0af-8c5f-4721-99bb-4aaf8fc4822e"),
                columns: new[] { "available_stock", "catalog_brand_id", "catalog_type_id" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("970afb7e-5616-46c4-b3fa-f9abb62928db"),
                columns: new[] { "available_stock", "catalog_brand_id", "catalog_type_id" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "id",
                keyValue: new Guid("bc047267-5eb9-4681-b489-4473ccd00a87"),
                columns: new[] { "available_stock", "catalog_brand_id", "catalog_type_id" },
                values: new object[] { 0, null, null });

            migrationBuilder.CreateIndex(
                name: "ix_products_catalog_brand_id",
                table: "products",
                column: "catalog_brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_catalog_type_id",
                table: "products",
                column: "catalog_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_products_catalog_brands_catalog_brand_id",
                table: "products",
                column: "catalog_brand_id",
                principalTable: "catalog_brands",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_products_catalog_types_catalog_type_id",
                table: "products",
                column: "catalog_type_id",
                principalTable: "catalog_types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_catalog_brands_catalog_brand_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_catalog_types_catalog_type_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "catalog_brands");

            migrationBuilder.DropTable(
                name: "catalog_types");

            migrationBuilder.DropIndex(
                name: "ix_products_catalog_brand_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_catalog_type_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "available_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "catalog_brand_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "catalog_type_id",
                table: "products");
        }
    }
}
