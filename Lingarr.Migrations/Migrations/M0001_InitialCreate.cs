using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(1)]
public class M0001_InitialCreate : Migration
{
    public override void Up()
    {
        // Settings
        Create.Table("settings")
            .WithColumn("key").AsString(255).PrimaryKey()
            .WithColumn("value").AsCustom("TEXT").NotNullable();

        // Movies
        Create.Table("movies")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("radarr_id").AsInt32().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("file_name").AsString().Nullable()
            .WithColumn("path").AsString().Nullable()
            .WithColumn("media_hash").AsString().Nullable()
            .WithColumn("date_added").AsDateTime().Nullable()
            .WithColumn("exclude_from_translation").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("translation_age_threshold").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Shows
        Create.Table("shows")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("sonarr_id").AsInt32().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("path").AsString().NotNullable()
            .WithColumn("date_added").AsDateTime().Nullable()
            .WithColumn("exclude_from_translation").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("translation_age_threshold").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Seasons
        Create.Table("seasons")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("season_number").AsInt32().NotNullable()
            .WithColumn("path").AsString().Nullable()
            .WithColumn("show_id").AsInt32()
                .NotNullable()
                .ForeignKey("fk_seasons_shows_show_id", "shows", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("exclude_from_translation").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_seasons_show_id").OnTable("seasons").OnColumn("show_id");

        // Episodes
        Create.Table("episodes")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("sonarr_id").AsInt32().NotNullable()
            .WithColumn("episode_number").AsInt32().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("file_name").AsString().Nullable()
            .WithColumn("path").AsString().Nullable()
            .WithColumn("media_hash").AsString().Nullable()
            .WithColumn("season_id").AsInt32()
                .NotNullable()
                .ForeignKey("fk_episodes_seasons_season_id", "seasons", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("exclude_from_translation").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("date_added").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_episodes_season_id").OnTable("episodes").OnColumn("season_id");

        // Images
        Create.Table("images")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("path").AsString().NotNullable()
            .WithColumn("show_id")
                .AsInt32()
                .Nullable()
                .ForeignKey("fk_images_shows_show_id", "shows", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("movie_id")
                .AsInt32()
                .Nullable()
                .ForeignKey("fk_images_movies_movie_id", "movies", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.Index("ix_images_show_id").OnTable("images").OnColumn("show_id");
        Create.Index("ix_images_movie_id").OnTable("images").OnColumn("movie_id");

        // Path mappings
        Create.Table("path_mappings")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("source_path").AsString().NotNullable()
            .WithColumn("destination_path").AsString().NotNullable()
            .WithColumn("media_type").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Translation
        Create.Table("translation_requests")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("job_id").AsString().Nullable()
            .WithColumn("media_id").AsInt32().Nullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("source_language").AsString().NotNullable()
            .WithColumn("target_language").AsString().NotNullable()
            .WithColumn("subtitle_to_translate").AsString().Nullable()
            .WithColumn("translated_subtitle").AsString().Nullable()
            .WithColumn("media_type").AsInt32().NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("completed_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Statistics
        Create.Table("statistics")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("total_lines_translated").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("total_files_translated").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("total_characters_translated").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("total_movies").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("total_episodes").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("total_subtitles").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("translations_by_media_type_json").AsString().NotNullable().WithDefaultValue("{}")
            .WithColumn("translations_by_service_json").AsString().NotNullable().WithDefaultValue("{}")
            .WithColumn("subtitles_by_language_json").AsString().NotNullable().WithDefaultValue("{}")
            .WithColumn("translations_by_model_json").AsString().NotNullable().WithDefaultValue("{}")
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Daily statistics
        Create.Table("daily_statistics")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("date").AsDateTime().NotNullable()
            .WithColumn("translation_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        // Users
        Create.Table("users")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("username").AsString(100).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("last_login_at").AsDateTime().Nullable();
    }

    public override void Down()
    {
        Delete.Table("users");
        Delete.Table("daily_statistics");
        Delete.Table("statistics");
        Delete.Table("translation_requests");
        Delete.Table("path_mappings");
        Delete.Table("images");
        Delete.Table("episodes");
        Delete.Table("seasons");
        Delete.Table("shows");
        Delete.Table("movies");
        Delete.Table("settings");
    }
}
