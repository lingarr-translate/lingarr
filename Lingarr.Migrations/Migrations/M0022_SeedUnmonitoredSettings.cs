using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedUnmonitoredSettings : Migration
{
    public override void Up()
    {
        Execute.Sql("INSERT INTO settings (key, value) SELECT 'radarr_exclude_unmonitored', 'false' WHERE NOT EXISTS (SELECT 1 FROM settings WHERE key = 'radarr_exclude_unmonitored')");
        Execute.Sql("INSERT INTO settings (key, value) SELECT 'sonarr_exclude_unmonitored_series', 'false' WHERE NOT EXISTS (SELECT 1 FROM settings WHERE key = 'sonarr_exclude_unmonitored_series')");
        Execute.Sql("INSERT INTO settings (key, value) SELECT 'sonarr_exclude_unmonitored_episodes', 'false' WHERE NOT EXISTS (SELECT 1 FROM settings WHERE key = 'sonarr_exclude_unmonitored_episodes')");
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "radarr_exclude_unmonitored" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_series" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_episodes" });
    }
}
