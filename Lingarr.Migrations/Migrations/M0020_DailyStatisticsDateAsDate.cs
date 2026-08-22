using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(20)]
public class M0020_DailyStatisticsDateAsDate : Migration
{
    public override void Up()
    {
        // The values are already midnight aligned, so narrowing to a date drops a zero time.
        IfDatabase("sqlite").Execute.Sql(
            "UPDATE daily_statistics SET date = substr(date, 1, 10)");

        IfDatabase("mysql").Alter.Table("daily_statistics")
            .AlterColumn("date").AsDate().NotNullable();

        IfDatabase("postgresql").Execute.Sql(
            "ALTER TABLE daily_statistics ALTER COLUMN date TYPE date USING date::date");
    }

    public override void Down()
    {
        IfDatabase("mysql").Alter.Table("daily_statistics")
            .AlterColumn("date").AsDateTime().NotNullable();

        IfDatabase("postgresql").Execute.Sql(
            "ALTER TABLE daily_statistics ALTER COLUMN date TYPE timestamp USING date::timestamp");
    }
}
