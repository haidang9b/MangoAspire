using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coupons.API.Data.Migrations;

/// <inheritdoc />
public partial class SeedData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "coupons",
            columns: new[] { "id", "code", "discount_amount" },
            values: new object[,]
            {
                { new Guid("50aa664d-2984-4159-a0ae-4e3eeaf416c1"), "30OFF", 30m },
                { new Guid("9bb19343-3ba8-492c-8786-469ec240d266"), "20OFF", 20m },
                { new Guid("b5a6b6d6-8c2f-4b2e-8f3e-2f5d3e4f5d3e"), "40OFF", 40m },
                { new Guid("fcc1596e-7dd5-486f-a498-8eacad314302"), "10OFF", 10m }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "coupons",
            keyColumn: "id",
            keyValue: new Guid("50aa664d-2984-4159-a0ae-4e3eeaf416c1"));

        migrationBuilder.DeleteData(
            table: "coupons",
            keyColumn: "id",
            keyValue: new Guid("9bb19343-3ba8-492c-8786-469ec240d266"));

        migrationBuilder.DeleteData(
            table: "coupons",
            keyColumn: "id",
            keyValue: new Guid("b5a6b6d6-8c2f-4b2e-8f3e-2f5d3e4f5d3e"));

        migrationBuilder.DeleteData(
            table: "coupons",
            keyColumn: "id",
            keyValue: new Guid("fcc1596e-7dd5-486f-a498-8eacad314302"));
    }
}
