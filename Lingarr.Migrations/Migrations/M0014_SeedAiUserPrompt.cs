using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(14)]
public class M0014_SeedAiUserPrompt : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "ai_user_prompt", value = "{lineToTranslate}" });

        IfDatabase("sqlite", "postgres").Execute.Sql("""
            UPDATE settings SET "value" =
                (SELECT prompt."value" FROM settings prompt WHERE prompt."key" = 'ai_context_prompt')
            WHERE "key" = 'ai_user_prompt'
              AND EXISTS (SELECT 1 FROM settings prompt
                          WHERE prompt."key" = 'ai_context_prompt')
              AND EXISTS (SELECT 1 FROM settings enabled
                          WHERE enabled."key" = 'ai_context_prompt_enabled'
                            AND enabled."value" = 'true')
            """);

        IfDatabase("mysql").Execute.Sql("""
            UPDATE settings target
            JOIN settings prompt ON prompt.`key` = 'ai_context_prompt'
            JOIN settings enabled ON enabled.`key` = 'ai_context_prompt_enabled'
                                 AND enabled.`value` = 'true'
            SET target.`value` = prompt.`value`
            WHERE target.`key` = 'ai_user_prompt'
            """);

        Delete.FromTable("settings").Row(new { key = "ai_context_prompt" });
        Delete.FromTable("settings").Row(new { key = "ai_context_prompt_enabled" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "ai_user_prompt" });
        Insert.IntoTable("settings").Row(new { key = "ai_context_prompt_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "ai_context_prompt", value = "Use the CONTEXT to translate the TARGET line.\n\n[TARGET] {lineToTranslate}\n\n[CONTEXT]\n{contextBefore}\n{lineToTranslate}\n{contextAfter}\n[/CONTEXT]" });
    }
}
