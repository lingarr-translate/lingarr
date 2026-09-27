using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(21)]
public class M0021_SeedAiContextUseTranslated : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "ai_context_use_translated", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "ai_context_use_translated" });
    }
}
