using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(5)]
public class M0005_SeedRequestTemplates : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "openai_request_template", value = "" });
        Insert.IntoTable("settings").Row(new { key = "anthropic_request_template", value = "" });
        Insert.IntoTable("settings").Row(new { key = "local_ai_chat_request_template", value = "" });
        Insert.IntoTable("settings").Row(new { key = "local_ai_generate_request_template", value = "" });
        Insert.IntoTable("settings").Row(new { key = "deepseek_request_template", value = "" });
        Insert.IntoTable("settings").Row(new { key = "gemini_request_template", value = "" });
        
        Delete.FromTable("settings").Row(new { key = "custom_ai_parameters" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "openai_request_template" });
        Delete.FromTable("settings").Row(new { key = "anthropic_request_template" });
        Delete.FromTable("settings").Row(new { key = "local_ai_chat_request_template" });
        Delete.FromTable("settings").Row(new { key = "local_ai_generate_request_template" });
        Delete.FromTable("settings").Row(new { key = "deepseek_request_template" });
        Delete.FromTable("settings").Row(new { key = "gemini_request_template" });
    }
}
