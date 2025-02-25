using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class AddParametersTagsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "gemini_model", "" },
                    { "gemini_api_key", "" },
                    { "deepseek_model", "" },
                    { "deepseek_api_key", "" },
                    { "movie_age_threshold", "0" },
                    { "show_age_threshold", "0" },
                    { "fix_overlapping_subtitles", "false" }
                });
            
            migrationBuilder.AddColumn<bool>(
                name: "exclude_from_translation",
                table: "shows",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "translation_age_threshold",
                table: "shows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "exclude_from_translation",
                table: "seasons",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "exclude_from_translation",
                table: "movies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "translation_age_threshold",
                table: "movies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "exclude_from_translation",
                table: "episodes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "gemini_model",
                    "gemini_api_key",
                    "deepseek_model",
                    "deepseek_api_key",
                    "movie_age_threshold",
                    "show_age_threshold",
                    "fix_overlapping_subtitles"
                });
            
            migrationBuilder.DropColumn(
                name: "exclude_from_translation",
                table: "shows");

            migrationBuilder.DropColumn(
                name: "translation_age_threshold",
                table: "shows");

            migrationBuilder.DropColumn(
                name: "exclude_from_translation",
                table: "seasons");

            migrationBuilder.DropColumn(
                name: "exclude_from_translation",
                table: "movies");

            migrationBuilder.DropColumn(
                name: "translation_age_threshold",
                table: "movies");

            migrationBuilder.DropColumn(
                name: "exclude_from_translation",
                table: "episodes");
        }
    }
}
