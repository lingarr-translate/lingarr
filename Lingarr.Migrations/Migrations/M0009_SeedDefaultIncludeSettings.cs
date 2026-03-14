using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(9)]
public class M0009_SeedDefaultIncludeSettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "radarr_default_include", value = "true" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_default_include", value = "true" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "radarr_default_include" });
        Delete.FromTable("settings").Row(new { key = "sonarr_default_include" });
    }
}
