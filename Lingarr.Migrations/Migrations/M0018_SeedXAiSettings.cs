using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(18)]
public class M0018_SeedXAiSettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new
        {
            key = "xai_model",
            value = ""
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "xai_api_key",
            value = ""
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "xai_request_template",
            value = ""
        });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new
        {
            key = "xai_model"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "xai_api_key"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "xai_request_template"
        });
    }
}
