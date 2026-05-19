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

        if (!Schema.Table("translation_request_lines").Column("matched_source").Exists())
        {
            Alter.Table("translation_request_lines")
                .AddColumn("matched_source")
                .AsString(50)
                .Nullable();
        }

        if (!Schema.Table("translation_request_lines").Column("matched_target").Exists())
        {
            Alter.Table("translation_request_lines")
                .AddColumn("matched_target")
                .AsString(50)
                .Nullable();
        }

        if (!Schema.Table("translation_request_lines").Column("tier").Exists())
        {
            Alter.Table("translation_request_lines")
                .AddColumn("tier")
                .AsInt32()
                .Nullable();
        }
    }

    public override void Down()
    {
        Delete.Column("service").FromTable("translation_request_lines");
        Delete.Column("matched_source").FromTable("translation_request_lines");
        Delete.Column("matched_target").FromTable("translation_request_lines");
        Delete.Column("tier").FromTable("translation_request_lines");
    }
}
