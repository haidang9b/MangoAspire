using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Data.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "order_headers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<string>(type: "text", nullable: false),
                coupon_code = table.Column<string>(type: "text", nullable: false),
                order_total = table.Column<decimal>(type: "numeric", nullable: false),
                discount_total = table.Column<decimal>(type: "numeric", nullable: false),
                first_name = table.Column<string>(type: "text", nullable: false),
                last_name = table.Column<string>(type: "text", nullable: false),
                pickup_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                order_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                phone = table.Column<string>(type: "text", nullable: true),
                email = table.Column<string>(type: "text", nullable: false),
                card_number = table.Column<string>(type: "text", nullable: false),
                cvv = table.Column<string>(type: "text", nullable: true),
                expiry_month_year = table.Column<string>(type: "text", nullable: true),
                cart_total_items = table.Column<int>(type: "integer", nullable: false),
                payment_status = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_order_headers", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "order_details",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_header_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                count = table.Column<int>(type: "integer", nullable: false),
                product_name = table.Column<string>(type: "text", nullable: true),
                price = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_order_details", x => x.id);
                table.ForeignKey(
                    name: "fk_order_details_order_headers_order_header_id",
                    column: x => x.order_header_id,
                    principalTable: "order_headers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_order_details_order_header_id",
            table: "order_details",
            column: "order_header_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "order_details");

        migrationBuilder.DropTable(
            name: "order_headers");
    }
}
