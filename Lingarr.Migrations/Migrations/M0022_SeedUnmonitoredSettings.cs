using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedUnmonitoredSettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "radarr_exclude_unmonitored", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_exclude_unmonitored_series", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_exclude_unmonitored_episodes", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "radarr_exclude_unmonitored" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_series" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_episodes" });
    }
}
