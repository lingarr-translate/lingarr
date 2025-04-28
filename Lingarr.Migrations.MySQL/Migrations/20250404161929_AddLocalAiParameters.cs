using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
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
                    { "local_ai_parameters", "[]" },
                    { "strip_subtitle_formatting", "false" },
                    { "subtitle_validation_enabled", "false" },
                    { "subtitle_validation_maxfilesizebytes", "2048" },
                    { "subtitle_validation_maxsubtitlelength", "500" },
                    { "subtitle_validation_minsubtitlelength", "2" },
                    { "subtitle_validation_mindurationms", "500" },
                    { "subtitle_validation_maxdurationsecs", "10" }
                });
            
            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "movies",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "movies",
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
            migrationBuilder.UpdateData(
                table: "movies",
                keyColumn: "path",
                keyValue: null,
                column: "path",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "movies",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "movies",
                keyColumn: "file_name",
                keyValue: null,
                column: "file_name",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "movies",
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
