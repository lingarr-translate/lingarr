using FluentMigrator.Runner;
using Lingarr.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lingarr.Migrations.Tests;

[Trait("Category", "Integration")]
public class DailyStatisticsDateUpgradeTests : IDisposable
{
    private const string ExistingRow = "2026-08-20 00:00:00";
    private static readonly DateOnly Expected = new(2026, 8, 20);
    private readonly List<string> _files = [];

    [Fact]
    public async Task Sqlite_KeepsTheDayWhenUpgrading()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        _files.Add(path);
        var connectionString = $"Data Source={path}";

        await Upgrade("sqlite", connectionString,
            builder => builder.UseSqlite(connectionString).UseSnakeCaseNamingConvention(),
            () => new SqliteConnection(connectionString));
    }

    [Fact]
    public async Task MySql_KeepsTheDayWhenUpgrading()
    {
        var token = TestContext.Current.CancellationToken;
        await using var container = new MySqlBuilder("mysql:latest").Build();
        await container.StartAsync(token);
        var connectionString = container.GetConnectionString();

        await Upgrade("mysql", connectionString,
            builder => builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .UseSnakeCaseNamingConvention(),
            () => new MySqlConnection(connectionString));
    }

    [Fact]
    public async Task Postgres_KeepsTheDayWhenUpgrading()
    {
        var token = TestContext.Current.CancellationToken;
        await using var container = new PostgreSqlBuilder("postgres:latest").Build();
        await container.StartAsync(token);
        var connectionString = container.GetConnectionString();

        await Upgrade("postgresql", connectionString,
            builder => builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(),
            () => new NpgsqlConnection(connectionString));
    }

    private static async Task Upgrade(
        string provider,
        string connectionString,
        Action<DbContextOptionsBuilder<LingarrDbContext>> configure,
        Func<System.Data.Common.DbConnection> openConnection)
    {
        var token = TestContext.Current.CancellationToken;

        // Stop at the schema an existing install is on, then write a row the old way.
        Migrate(connectionString, provider, 19);
        await using (var connection = openConnection())
        {
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO daily_statistics (date, translation_count, created_at, updated_at) " +
                $"VALUES ('{ExistingRow}', 7, '{ExistingRow}', '{ExistingRow}')";
            await command.ExecuteNonQueryAsync(token);
        }

        Migrate(connectionString, provider, null);

        var builder = new DbContextOptionsBuilder<LingarrDbContext>();
        configure(builder);
        await using (var context = new LingarrDbContext(builder.Options))
        {
            var row = await context.DailyStatistics.SingleAsync(token);

            Assert.Equal(Expected, row.Date);
            Assert.Equal(7, row.TranslationCount);
        }
    }

    private static void Migrate(string connectionString, string provider, long? version)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(connectionString, provider);
        using var scope = services.BuildServiceProvider().CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (version.HasValue)
        {
            runner.MigrateUp(version.Value);
        }
        else
        {
            runner.MigrateUp();
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in _files.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }
}
