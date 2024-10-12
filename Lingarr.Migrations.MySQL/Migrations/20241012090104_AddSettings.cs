using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
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
                    { "openai_model", "" },
                    { "openai_api_key", "" },
                    { "anthropic_api_key", "" },
                    { "anthropic_model", "" },
                    { "anthropic_version", "" }
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
