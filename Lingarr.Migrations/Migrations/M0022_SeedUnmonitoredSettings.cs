using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedUnmonitoredSettings : Migration
{
    public override void Up()
    {
        IfDatabase("sqlite").Execute.Sql("INSERT OR IGNORE INTO settings (key, value) VALUES ('radarr_exclude_unmonitored', 'false')");
        IfDatabase("sqlite").Execute.Sql("INSERT OR IGNORE INTO settings (key, value) VALUES ('sonarr_exclude_unmonitored_series', 'false')");
        IfDatabase("sqlite").Execute.Sql("INSERT OR IGNORE INTO settings (key, value) VALUES ('sonarr_exclude_unmonitored_episodes', 'false')");

        IfDatabase("mysql").Execute.Sql("INSERT IGNORE INTO settings (`key`, `value`) VALUES ('radarr_exclude_unmonitored', 'false'), ('sonarr_exclude_unmonitored_series', 'false'), ('sonarr_exclude_unmonitored_episodes', 'false')");

        IfDatabase("postgresql").Execute.Sql("INSERT INTO settings (key, value) VALUES ('radarr_exclude_unmonitored', 'false'), ('sonarr_exclude_unmonitored_series', 'false'), ('sonarr_exclude_unmonitored_episodes', 'false') ON CONFLICT (key) DO NOTHING");
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "radarr_exclude_unmonitored" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_series" });
        Delete.FromTable("settings").Row(new { key = "sonarr_exclude_unmonitored_episodes" });
    }
}
