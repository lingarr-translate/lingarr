using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(3)]
public class M0003_AddTranslationErrorMessage : Migration
{
    public override void Up()
    {
        Alter.Table("translation_requests")
            .AddColumn("error_message").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Column("error_message").FromTable("translation_requests");
    }
}
