using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(4)]
public class M0004_AlterStringsToText : Migration
{
    public override void Up()
    {
        // Settings
        Alter.Column("value").OnTable("settings").AsCustom("TEXT").NotNullable();

        // Movies
        Alter.Column("title").OnTable("movies").AsCustom("TEXT").NotNullable();
        Alter.Column("file_name").OnTable("movies").AsCustom("TEXT").Nullable();
        Alter.Column("path").OnTable("movies").AsCustom("TEXT").Nullable();
        Alter.Column("media_hash").OnTable("movies").AsCustom("TEXT").Nullable();

        // Shows
        Alter.Column("title").OnTable("shows").AsCustom("TEXT").NotNullable();
        Alter.Column("path").OnTable("shows").AsCustom("TEXT").NotNullable();

        // Seasons
        Alter.Column("path").OnTable("seasons").AsCustom("TEXT").Nullable();

        // Episodes
        Alter.Column("title").OnTable("episodes").AsCustom("TEXT").NotNullable();
        Alter.Column("file_name").OnTable("episodes").AsCustom("TEXT").Nullable();
        Alter.Column("path").OnTable("episodes").AsCustom("TEXT").Nullable();
        Alter.Column("media_hash").OnTable("episodes").AsCustom("TEXT").Nullable();

        // Images
        Alter.Column("type").OnTable("images").AsCustom("TEXT").NotNullable();
        Alter.Column("path").OnTable("images").AsCustom("TEXT").NotNullable();

        // Path mappings
        Alter.Column("source_path").OnTable("path_mappings").AsCustom("TEXT").NotNullable();
        Alter.Column("destination_path").OnTable("path_mappings").AsCustom("TEXT").NotNullable();

        // Translation requests
        Alter.Column("job_id").OnTable("translation_requests").AsCustom("TEXT").Nullable();
        Alter.Column("title").OnTable("translation_requests").AsCustom("TEXT").NotNullable();
        Alter.Column("source_language").OnTable("translation_requests").AsCustom("TEXT").NotNullable();
        Alter.Column("target_language").OnTable("translation_requests").AsCustom("TEXT").NotNullable();
        Alter.Column("subtitle_to_translate").OnTable("translation_requests").AsCustom("TEXT").Nullable();
        Alter.Column("translated_subtitle").OnTable("translation_requests").AsCustom("TEXT").Nullable();

        // Statistics
        Alter.Column("translations_by_media_type_json").OnTable("statistics").AsCustom("TEXT").NotNullable();
        Alter.Column("translations_by_service_json").OnTable("statistics").AsCustom("TEXT").NotNullable();
        Alter.Column("subtitles_by_language_json").OnTable("statistics").AsCustom("TEXT").NotNullable();
        Alter.Column("translations_by_model_json").OnTable("statistics").AsCustom("TEXT").NotNullable();

        // Users
        Alter.Column("username").OnTable("users").AsCustom("TEXT").NotNullable();
        Alter.Column("password_hash").OnTable("users").AsCustom("TEXT").NotNullable();
    }

    public override void Down()
    {
        //
    }
}
