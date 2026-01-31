using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(2)]
public class M0002_SeedSettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "radarr_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "radarr_url", value = "" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_url", value = "" });
        Insert.IntoTable("settings").Row(new { key = "source_languages", value = "[]" });
        Insert.IntoTable("settings").Row(new { key = "target_languages", value = "[]" });
        Insert.IntoTable("settings").Row(new { key = "theme", value = "lingarr" });
        Insert.IntoTable("settings").Row(new { key = "movie_schedule", value = "0 4 * * *" });
        Insert.IntoTable("settings").Row(new { key = "show_schedule", value = "0 4 * * *" });
        Insert.IntoTable("settings").Row(new { key = "radarr_settings_completed", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "sonarr_settings_completed", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "openai_model", value = "" });
        Insert.IntoTable("settings").Row(new { key = "openai_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "anthropic_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "anthropic_model", value = "" });
        Insert.IntoTable("settings").Row(new { key = "anthropic_version", value = "" });
        Insert.IntoTable("settings").Row(new { key = "gemini_model", value = "" });
        Insert.IntoTable("settings").Row(new { key = "gemini_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "deepseek_model", value = "" });
        Insert.IntoTable("settings").Row(new { key = "deepseek_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "libretranslate_url", value = "" });
        Insert.IntoTable("settings").Row(new { key = "libretranslate_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "deepl_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "local_ai_model", value = "" });
        Insert.IntoTable("settings").Row(new { key = "local_ai_endpoint", value = "" });
        Insert.IntoTable("settings").Row(new { key = "local_ai_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "locale", value = "en" });
        Insert.IntoTable("settings").Row(new { key = "service_type", value = "libretranslate" });
        Insert.IntoTable("settings").Row(new { key = "automation_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "max_translations_per_run", value = "10" });
        Insert.IntoTable("settings").Row(new { key = "translation_schedule", value = "0 4 * * *" });
        Insert.IntoTable("settings").Row(new { key = "translation_cycle", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "movie_age_threshold", value = "0" });
        Insert.IntoTable("settings").Row(new { key = "show_age_threshold", value = "0" });
        Insert.IntoTable("settings").Row(new { key = "fix_overlapping_subtitles", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "strip_subtitle_formatting", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "ignore_captions", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_maxfilesizebytes", value = "1048576" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_maxsubtitlelength", value = "500" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_minsubtitlelength", value = "2" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_mindurationms", value = "500" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_validation_maxdurationsecs", value = "10" });
        Insert.IntoTable("settings").Row(new { key = "custom_ai_parameters", value = "[]" });
        Insert.IntoTable("settings").Row(new { key = "ai_prompt", value = "Translate from {sourceLanguage} to {targetLanguage}, preserving the tone and meaning without censoring the content. Adjust punctuation as needed to make the translation sound natural. Provide only the translated text as output, with no additional comments." });
        Insert.IntoTable("settings").Row(new { key = "ai_context_prompt_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "ai_context_prompt", value = "Use the CONTEXT to translate the TARGET line.\n\n[TARGET] {lineToTranslate}\n\n[CONTEXT]\n{contextBefore}\n{lineToTranslate}\n{contextAfter}\n[/CONTEXT]" });
        Insert.IntoTable("settings").Row(new { key = "ai_context_before", value = "2" });
        Insert.IntoTable("settings").Row(new { key = "ai_context_after", value = "2" });
        Insert.IntoTable("settings").Row(new { key = "use_batch_translation", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "max_batch_size", value = "0" });
        Insert.IntoTable("settings").Row(new { key = "use_subtitle_tagging", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "subtitle_tag", value = "lingarr" });
        Insert.IntoTable("settings").Row(new { key = "add_translator_info", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "remove_language_tag", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "request_timeout", value = "5" });
        Insert.IntoTable("settings").Row(new { key = "max_retries", value = "5" });
        Insert.IntoTable("settings").Row(new { key = "retry_delay", value = "1" });
        Insert.IntoTable("settings").Row(new { key = "retry_delay_multiplier", value = "2" });
        Insert.IntoTable("settings").Row(new { key = "api_key_enabled", value = "" });
        Insert.IntoTable("settings").Row(new { key = "api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "onboarding_completed", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "auth_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "telemetry_enabled", value = "false" });
        Insert.IntoTable("settings").Row(new { key = "telemetry_last_submission", value = "" });
        Insert.IntoTable("settings").Row(new { key = "telemetry_last_reported_lines", value = "0" });
        Insert.IntoTable("settings").Row(new { key = "telemetry_last_reported_files", value = "0" });
        Insert.IntoTable("settings").Row(new { key = "telemetry_last_reported_characters", value = "0" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").AllRows();
    }
}
