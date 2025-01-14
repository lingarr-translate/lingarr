using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "locale", "en" }
                });
            
            migrationBuilder.CreateTable(
                name: "daily_statistics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    translation_count = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_statistics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "statistics",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    total_lines_translated = table.Column<long>(type: "INTEGER", nullable: false),
                    total_files_translated = table.Column<long>(type: "INTEGER", nullable: false),
                    total_characters_translated = table.Column<long>(type: "INTEGER", nullable: false),
                    total_movies = table.Column<int>(type: "INTEGER", nullable: false),
                    total_episodes = table.Column<int>(type: "INTEGER", nullable: false),
                    total_subtitles = table.Column<int>(type: "INTEGER", nullable: false),
                    translations_by_media_type_json = table.Column<string>(type: "TEXT", nullable: false),
                    translations_by_service_json = table.Column<string>(type: "TEXT", nullable: false),
                    subtitles_by_language_json = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_statistics", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "locale"
                });
            
            migrationBuilder.DropTable(
                name: "daily_statistics");

            migrationBuilder.DropTable(
                name: "statistics");
        }
    }
}
