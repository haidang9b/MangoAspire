using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Products.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDebeziumSignalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "debezium_signal",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_debezium_signal", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "debezium_signal");
        }
    }
}
