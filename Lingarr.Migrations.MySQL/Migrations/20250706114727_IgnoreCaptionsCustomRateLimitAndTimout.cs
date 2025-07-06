using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.MySQL.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreCaptionsCustomRateLimitAndTimout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "ignore_captions", "false" },
                    { "request_timeout", "5" },
                    { "max_retries", "5" },
                    { "retry_delay", "1" },
                    { "retry_delay_multiplier", "2" }
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
                    "ignore_captions",
                    "request_timeout",
                    "max_retries",
                    "retry_delay",
                    "retry_delay_multiplier"
                });
        }
    }
}
