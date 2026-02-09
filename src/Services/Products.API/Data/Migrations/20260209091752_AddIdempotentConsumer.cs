using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Products.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotentConsumer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processed_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_message", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_processed_message_id_name",
                table: "processed_message",
                columns: new[] { "id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_message");
        }
    }
}
