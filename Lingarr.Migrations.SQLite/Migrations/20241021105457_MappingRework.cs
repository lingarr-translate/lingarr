using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class MappingRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "path_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    source_path = table.Column<string>(type: "TEXT", nullable: false),
                    destination_path = table.Column<string>(type: "TEXT", nullable: false),
                    media_type = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_path_mappings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "path_mappings");
        }
    }
}
