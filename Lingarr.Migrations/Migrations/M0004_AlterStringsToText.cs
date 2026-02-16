using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(4)]
public class M0004_AlterStringsToText : Migration
{
    public override void Up()
    {
        // SQLite already uses TEXT affinity for string columns, so these alterations
        // are only needed for MySQL (VARCHAR→TEXT) and PostgreSQL.

        AlterToText("settings", "value");

        AlterToText("movies", "title");
        AlterToText("movies", "file_name", nullable: true);
        AlterToText("movies", "path", nullable: true);
        AlterToText("movies", "media_hash", nullable: true);

        AlterToText("shows", "title");
        AlterToText("shows", "path");

        AlterToText("seasons", "path", nullable: true);

        AlterToText("episodes", "title");
        AlterToText("episodes", "file_name", nullable: true);
        AlterToText("episodes", "path", nullable: true);
        AlterToText("episodes", "media_hash", nullable: true);

        AlterToText("images", "type");
        AlterToText("images", "path");

        AlterToText("path_mappings", "source_path");
        AlterToText("path_mappings", "destination_path");

        AlterToText("translation_requests", "job_id", nullable: true);
        AlterToText("translation_requests", "title");
        AlterToText("translation_requests", "source_language");
        AlterToText("translation_requests", "target_language");
        AlterToText("translation_requests", "subtitle_to_translate", nullable: true);
        AlterToText("translation_requests", "translated_subtitle", nullable: true);

        AlterToText("statistics", "translations_by_media_type_json");
        AlterToText("statistics", "translations_by_service_json");
        AlterToText("statistics", "subtitles_by_language_json");
        AlterToText("statistics", "translations_by_model_json");

        AlterToText("users", "username");
        AlterToText("users", "password_hash");
    }

    public override void Down()
    {
        //
    }

    private void AlterToText(string table, string column, bool nullable = false)
    {
        var alter = IfDatabase("mysql", "postgres")
            .Alter.Column(column).OnTable(table).AsCustom("TEXT");

        if (nullable)
            alter.Nullable();
        else
            alter.NotNullable();
    }
}
