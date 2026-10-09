using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_AddTranslationRequestCaption : Migration
{
    public override void Up()
    {
        Alter.Table("translation_requests")
            .AddColumn("caption").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Column("caption").FromTable("translation_requests");
    }
}
