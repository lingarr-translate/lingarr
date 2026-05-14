using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(11)]
public class M0011_SeedPreserveLineBreaks : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "preserve_line_breaks", value = "false" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "preserve_line_breaks" });
    }
}
