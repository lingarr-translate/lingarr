using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(3)]
public class M0003_AddTranslationRequestEvents : Migration
{
    public override void Up()
    {
        Alter.Table("translation_requests")
            .AddColumn("error_message").AsCustom("TEXT").Nullable()
            .AddColumn("stack_trace").AsCustom("TEXT").Nullable();

        Create.Table("translation_request_events")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("translation_request_id").AsInt32()
                .NotNullable()
                .ForeignKey("fk_translation_request_events_translation_requests_id", "translation_requests", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("message").AsCustom("TEXT").Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_translation_request_events_translation_request_id")
            .OnTable("translation_request_events")
            .OnColumn("translation_request_id");

        Create.Table("translation_request_lines")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("translation_request_id").AsInt32()
                .NotNullable()
                .ForeignKey("fk_translation_request_lines_translation_requests_id", "translation_requests", "id")
                .OnDeleteOrUpdate(System.Data.Rule.Cascade)
            .WithColumn("position").AsInt32().NotNullable()
            .WithColumn("source").AsCustom("TEXT").NotNullable()
            .WithColumn("target").AsCustom("TEXT").NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_translation_request_lines_translation_request_id")
            .OnTable("translation_request_lines")
            .OnColumn("translation_request_id");
        
        Insert.IntoTable("settings").Row(new { key = "navigate_to_details_on_request", value = "true" });
    }

    public override void Down()
    {
        Delete.Table("translation_request_lines");
        Delete.Table("translation_request_events");

        Delete.Column("error_message").FromTable("translation_requests");
        Delete.Column("stack_trace").FromTable("translation_requests");
    }
}
