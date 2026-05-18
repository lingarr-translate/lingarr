using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(12)]
public class M0012_TranslationServices : Migration
{
    public override void Up()
    {
        if (!Schema.Table("translation_request_lines").Column("service").Exists())
        {
            Alter.Table("translation_request_lines")
                .AddColumn("service")
                .AsString(50)
                .Nullable();
        }
    }

    public override void Down()
    {
        Delete.Column("service").FromTable("translation_request_lines");
    }
}
