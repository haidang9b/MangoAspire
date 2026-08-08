using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAgent.App.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCdcReplayFenceAndStreamOffsets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "source_lsn",
                table: "products",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "source_timestamp",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "source_lsn",
                table: "product_categories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "source_timestamp",
                table: "product_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cdc_stream_offsets",
                columns: table => new
                {
                    stream_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    offset = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cdc_stream_offsets", x => x.stream_name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cdc_stream_offsets");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "products");

            migrationBuilder.DropColumn(
                name: "source_lsn",
                table: "products");

            migrationBuilder.DropColumn(
                name: "source_timestamp",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "source_lsn",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "source_timestamp",
                table: "product_categories");
        }
    }
}
