using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(7)]
public class M0007_SeedLanguageCodeFormat : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "language_code_format", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "language_code_format" });
    }
}
