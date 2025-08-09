using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslatorInfoAndRemoveLanguageTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "add_translator_info", "false" },
                    { "remove_language_tag", "false" },
                    { "api_key_enabled", "false" },
                    { "api_key", "" }
                });
            
            migrationBuilder.AlterColumn<string>(
                    name: "subtitle_to_translate",
                    table: "translation_requests",
                    type: "longtext",
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "add_translator_info",
                    "remove_language_tag",
                    "api_key_enabled",
                    "api_key"
                });

            migrationBuilder.AlterColumn<string>(
                    name: "subtitle_to_translate",
                    table: "translation_requests",
                    type: "longtext",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "longtext",
                    oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
