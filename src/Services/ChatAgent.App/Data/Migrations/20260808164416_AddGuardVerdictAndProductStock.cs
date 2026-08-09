using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAgent.App.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardVerdictAndProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "available_stock",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "content_flagged",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Postgres refuses to narrow text to varchar(4000) if a single existing row is
            // longer, and the column was previously unbounded while assistant answers are
            // model-generated. Without this the migration fails on any database that has been
            // used, which is every database that matters.
            migrationBuilder.Sql(
                "UPDATE chat_messages SET content = left(content, 4000) WHERE length(content) > 4000;");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "chat_messages",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "review_verdict",
                table: "chat_messages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "content_flagged",
                table: "products");

            migrationBuilder.DropColumn(
                name: "review_verdict",
                table: "chat_messages");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "chat_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);
        }
    }
}
