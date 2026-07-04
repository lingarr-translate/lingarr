using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(13)]
public class M0013_AddPluginSettingProvider : Migration
{
    public override void Up()
    {
        if (!Schema.Table("settings").Column("provider").Exists())
        {
            Alter.Table("settings")
                .AddColumn("provider")
                .AsString(255)
                .Nullable();
        }
    }

    public override void Down()
    {
        if (Schema.Table("settings").Column("provider").Exists())
        {
            Delete.Column("provider").FromTable("settings");
        }
    }
}
