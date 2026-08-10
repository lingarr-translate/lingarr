using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(17)]
public class M0017_SeedMistralSettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new
        {
            key = "mistral_model", 
            value = ""
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "mistral_api_key", 
            value = ""
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "mistral_request_template", 
            value = ""
        });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new
        {
            key = "mistral_model"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "mistral_api_key"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "mistral_request_template"
        });
    }
}
