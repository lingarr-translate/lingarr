using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(10)]
public class M0010_DropLegacyExcludeColumnSqlite : Migration
{
    private static readonly string[] Tables = ["movies", "shows", "seasons", "episodes"];

    public override void Up()
    {
        foreach (var table in Tables)
        {
            if (Schema.Table(table).Column("exclude_from_translation").Exists())
            {
                IfDatabase("sqlite")
                    .Delete.Column("exclude_from_translation").FromTable(table);
            }
        }
    }

    public override void Down()
    {
        foreach (var table in Tables)
        {
            IfDatabase("sqlite")
                .Alter.Table(table)
                .AddColumn("exclude_from_translation")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

            IfDatabase("sqlite")
                .Execute.Sql($"UPDATE {table} SET exclude_from_translation = NOT include_in_translation");
        }
    }
}
