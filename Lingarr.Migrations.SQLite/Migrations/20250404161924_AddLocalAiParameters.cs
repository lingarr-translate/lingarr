using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalAiParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "custom_ai_parameters", "[]" },
                    { "strip_subtitle_formatting", "false" },
                    { "subtitle_validation_enabled", "false" },
                    { "subtitle_validation_maxfilesizebytes", "1048576" },
                    { "subtitle_validation_maxsubtitlelength", "500" },
                    { "subtitle_validation_minsubtitlelength", "2" },
                    { "subtitle_validation_mindurationms", "500" },
                    { "subtitle_validation_maxdurationsecs", "10" },
                    { "ai_context_prompt", "Translate the TARGET line from {sourceLanguage} to {targetLanguage}, preserving tone, intent, and cultural meaning. Do not censor content. Adjust punctuation naturally. Use the CONTEXT below to ensure the translation fits conversationally. Return only the translated TARGET line, no comments, no labels.\n\nContext:\n{contextBefore}\n[TARGET] {lineToTranslate}\n{contextAfter}\n" },
                    { "ai_context_prompt_enabled", "false" },
                    { "ai_context_before", "2" },
                    { "ai_context_after", "2" }
                });
            
            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "movies",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "movies",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "movies",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "movies",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
