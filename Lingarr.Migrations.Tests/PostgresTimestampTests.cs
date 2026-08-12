using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lingarr.Migrations.Tests;

[Trait("Category", "Integration")]
public class PostgresTimestampTests
{
    private const string DatabaseName = "lingarr";
    private const string ServerTimeZone = "Europe/Amsterdam";

    [Fact]
    public async Task Postgres_UtcTimestampRoundTripsUnderNonUtcServerTimeZone()
    {
        var token = TestContext.Current.CancellationToken;
        var written = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        await using var container = new PostgreSqlBuilder("postgres:latest")
            .WithDatabase(DatabaseName)
            .Build();
        await container.StartAsync(token);

        var connectionString = container.GetConnectionString();
        await SetDatabaseTimeZone(connectionString);
        RunMigrations(connectionString);

        int id;
        await using (var context = CreateContext(connectionString))
        {
            var statistics = new DailyStatistics { Date = written, TranslationCount = 1 };
            context.DailyStatistics.Add(statistics);
            await context.SaveChangesAsync(token);
            id = statistics.Id;
        }

        await using (var context = CreateContext(connectionString))
        {
            var reloaded = await context.DailyStatistics.SingleAsync(row => row.Id == id, token);
            Assert.Equal(written, reloaded.Date);
        }
    }

    private static async Task SetDatabaseTimeZone(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE {DatabaseName} SET TimeZone = '{ServerTimeZone}'";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        NpgsqlConnection.ClearPool(connection);
    }

    private static void RunMigrations(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(connectionString, "postgresql");

        MigrationConfiguration.RunMigrations(services.BuildServiceProvider());
    }

    private static LingarrDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LingarrDbContext(options);
    }
}
