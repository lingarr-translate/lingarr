using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(16)]
public class M0016_AddTranslationRequestJobType : Migration
{
    public override void Up()
    {
        if (!Schema.Table("translation_requests").Column("job_type").Exists())
        {
            Alter.Table("translation_requests")
                .AddColumn("job_type")
                .AsInt32()
                .Nullable();
        }
    }

    public override void Down()
    {
        Delete.Column("job_type").FromTable("translation_requests");
    }
}
