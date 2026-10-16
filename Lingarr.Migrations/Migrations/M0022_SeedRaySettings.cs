using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(22)]
public class M0022_SeedRaySettings : Migration
{
    public override void Up()
    {
        // Idempotent: Ray keys may already exist when upgrading from the old
        // Lingarr.Plugin.Ray DLL (same setting names).
        IfDatabase("mysql").Execute.Sql("""
            INSERT IGNORE INTO settings (`key`, `value`) VALUES
                ('ray_endpoint', 'http://127.0.0.1:8787'),
                ('ray_api_key', ''),
                ('ray_target', 'local'),
                ('ray_batch_size', '200'),
                ('ray_job_timeout_minutes', '180')
            """);

        IfDatabase("sqlite").Execute.Sql("""
            INSERT OR IGNORE INTO settings ("key", "value") VALUES
                ('ray_endpoint', 'http://127.0.0.1:8787'),
                ('ray_api_key', ''),
                ('ray_target', 'local'),
                ('ray_batch_size', '200'),
                ('ray_job_timeout_minutes', '180')
            """);

        IfDatabase("postgresql").Execute.Sql("""
            INSERT INTO settings ("key", "value") VALUES
                ('ray_endpoint', 'http://127.0.0.1:8787'),
                ('ray_api_key', ''),
                ('ray_target', 'local'),
                ('ray_batch_size', '200'),
                ('ray_job_timeout_minutes', '180')
            ON CONFLICT ("key") DO NOTHING
            """);
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
