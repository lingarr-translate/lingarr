using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(19)]
public class M0019_SeedTelemetryInstallationId : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new
        {
            key = "telemetry_installation_id",
            value = ""
        });

        Delete.FromTable("settings").Row(new
        {
            key = "telemetry_last_reported_lines"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "telemetry_last_reported_files"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "telemetry_last_reported_characters"
        });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new
        {
            key = "telemetry_installation_id"
        });

        Insert.IntoTable("settings").Row(new
        {
            key = "telemetry_last_reported_lines",
            value = "0"
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "telemetry_last_reported_files",
            value = "0"
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "telemetry_last_reported_characters",
            value = "0"
        });
    }
}
