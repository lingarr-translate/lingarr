using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "openai_model", "gpt-4o-mini" },
                    { "openai_api_key", "" },
                    { "anthropic_api_key", "" },
                    { "anthropic_model", "claude-3-5-sonnet-20240620" },
                    { "anthropic_version", "2023-06-01" }
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
                    "openai_model",
                    "openai_api_key",
                    "anthropic_api_key",
                    "anthropic_model",
                    "anthropic_version"
                });
        }
    }
}
