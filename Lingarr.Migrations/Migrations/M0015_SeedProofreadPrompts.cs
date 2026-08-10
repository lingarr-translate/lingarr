using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(15)]
public class M0015_SeedProofreadPrompts : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new
        {
            key = "proofread_prompt", 
            value = "You are proofreading a subtitle translated from {sourceLanguage} to {targetLanguage}. Compare the translation against the source and correct mistranslations, wrong names, grammar and punctuation, preserving the tone and meaning without censoring the content. Keep the length close to the original so it still fits on screen. If the translation is already correct, repeat it unchanged. Provide only the corrected translation as output, with no additional comments."
        });
        Insert.IntoTable("settings").Row(new
        {
            key = "proofread_user_prompt", 
            value = "[SOURCE] {sourceLine}\n[TRANSLATION] {translatedLine}"
        });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new
        {
            key = "proofread_prompt"
        });
        Delete.FromTable("settings").Row(new
        {
            key = "proofread_user_prompt"
        });
    }
}
