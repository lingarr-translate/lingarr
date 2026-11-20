using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(24)]
public class M0024_SeedFirstTargetOnly : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "first_target_only", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "first_target_only" });
    }
}
