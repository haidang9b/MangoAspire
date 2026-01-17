using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coupons.API.Data.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "coupons",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "text", nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_coupons", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "coupons");
    }
}
