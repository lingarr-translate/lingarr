using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedLocalAiStructuredOutput : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "local_ai_structured_output", value = "true" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "local_ai_structured_output" });
    }
}
