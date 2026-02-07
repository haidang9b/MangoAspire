using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "order_headers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "order_headers");
        }
    }
}
