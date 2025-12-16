using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingarr.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "api_key_enabled",
                });
            
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "key", "value" },
                values: new object[,]
                {
                    { "onboarding_completed", "false" },
                    { "auth_enabled", "false" },
                    { "telemetry_enabled", "false" },
                    { "telemetry_last_submission", "" },
                    { "telemetry_last_reported_lines", "0" },
                    { "telemetry_last_reported_files", "0" },
                    { "telemetry_last_reported_characters", "0" }
                });
            
            migrationBuilder.AddColumn<DateTime>(
                name: "date_added",
                table: "episodes",
                type: "TEXT",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "translations_by_model_json",
                table: "statistics",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "key",
                keyValues: new object[]
                {
                    "onboarding_completed",
                    "auth_enabled",
                    "telemetry_enabled",
                    "telemetry_last_submission",
                    "telemetry_last_reported_lines",
                    "telemetry_last_reported_files",
                    "telemetry_last_reported_characters",
                });
            
            migrationBuilder.DropColumn(
                name: "date_added",
                table: "episodes");
            
            migrationBuilder.DropColumn(
                name: "translations_by_model_json",
                table: "statistics");
        }
    }
}
