using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(8)]
public class M0008_RenameExcludeToInclude : Migration
{
    private static readonly string[] Tables = ["movies", "shows", "seasons", "episodes"];

    public override void Up()
    {
        foreach (var table in Tables)
        {
            // Add new column with correct default (true = included in translation by default)
            Alter.Table(table)
                .AddColumn("include_in_translation")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            // Copy inverted values from old column
            Execute.Sql($"UPDATE {table} SET include_in_translation = NOT exclude_from_translation");
        }

        // Drop old columns on MySQL/PostgreSQL; SQLite keeps them harmlessly
        foreach (var table in Tables)
        {
            IfDatabase("mysql", "postgres")
                .Delete.Column("exclude_from_translation").FromTable(table);
        }
    }

    public override void Down()
    {
        foreach (var table in Tables)
        {
            Alter.Table(table)
                .AddColumn("exclude_from_translation")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            Execute.Sql($"UPDATE {table} SET exclude_from_translation = NOT include_in_translation");
        }

        foreach (var table in Tables)
        {
            IfDatabase("mysql", "postgres")
                .Delete.Column("include_in_translation").FromTable(table);
        }
    }
}
