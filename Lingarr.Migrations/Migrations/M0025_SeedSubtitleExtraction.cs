using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(25)]
public class M0025_SeedSubtitleExtraction : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "subtitle_extraction_enabled", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "subtitle_extraction_enabled" });
    }
}
