using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class PopulateSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "radarr_api_key", "" },
                    { "radarr_url", "" },
                    { "sonarr_api_key", "" },
                    { "sonarr_url", "" },
                    { "source_languages", "[]" },
                    { "target_languages", "[]" },
                    { "theme", "lingarr" },
                    { "movie_schedule", "0 4 * * *" },
                    { "show_schedule", "0 4 * * *" }
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
                    "radarr_api_key",
                    "radarr_url",
                    "sonarr_api_key",
                    "sonarr_url",
                    "source_languages",
                    "target_languages",
                    "theme",
                    "movie_schedule",
                    "show_schedule"
                });
        }
    }
}
