using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedRaySettings : Migration
{
    public override void Up()
    {
        Insert.IntoTable("settings").Row(new { key = "ray_endpoint", value = "http://127.0.0.1:8787" });
        Insert.IntoTable("settings").Row(new { key = "ray_api_key", value = "" });
        Insert.IntoTable("settings").Row(new { key = "ray_target", value = "local" });
        Insert.IntoTable("settings").Row(new { key = "ray_batch_size", value = "200" });
        Insert.IntoTable("settings").Row(new { key = "ray_job_timeout_minutes", value = "180" });
    }

    public override void Down()
    {
        Delete.FromTable("settings").Row(new { key = "ray_endpoint" });
        Delete.FromTable("settings").Row(new { key = "ray_api_key" });
        Delete.FromTable("settings").Row(new { key = "ray_target" });
        Delete.FromTable("settings").Row(new { key = "ray_batch_size" });
        Delete.FromTable("settings").Row(new { key = "ray_job_timeout_minutes" });
    }
}
