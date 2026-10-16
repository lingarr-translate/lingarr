using FluentMigrator.Runner;
using Lingarr.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lingarr.Migrations.Tests;

[Trait("Category", "Integration")]
public class MigrationTests
{
    private static void RunMigrations(string connectionString, string dbType)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(connectionString, dbType);

        var serviceProvider = services.BuildServiceProvider();
        MigrationConfiguration.RunMigrations(serviceProvider);
    }

    [Fact]
    public async Task Sqlite_MigrationsRunSuccessfully()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr_test_{Guid.NewGuid()}.db");
        try
        {
            var connectionString = $"Data Source={dbPath}";
            RunMigrations(connectionString, "sqlite");

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Sqlite_MigrationsRollBackSuccessfully()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr_test_{Guid.NewGuid()}.db");
        try
        {
            var connectionString = $"Data Source={dbPath}";
            var services = new ServiceCollection();
            services.AddFluentMigrator(connectionString, "sqlite");

            var serviceProvider = services.BuildServiceProvider();
            MigrationConfiguration.RunMigrations(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateDown(7);

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Sqlite_MigrationsReapplyAfterRollback()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr_test_{Guid.NewGuid()}.db");
        try
        {
            var connectionString = $"Data Source={dbPath}";
            var services = new ServiceCollection();
            services.AddFluentMigrator(connectionString, "sqlite");

            var serviceProvider = services.BuildServiceProvider();
            MigrationConfiguration.RunMigrations(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateDown(2);
            runner.MigrateUp();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM settings WHERE key = 'navigate_to_details_on_request'";
            Assert.Equal(1L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Sqlite_UnmonitoredSettingsAreSeededWithFalseDefaults()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr_test_{Guid.NewGuid()}.db");
        try
        {
            var connectionString = $"Data Source={dbPath}";
            RunMigrations(connectionString, "sqlite");

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            var expectedSettings = new Dictionary<string, string>
            {
                ["radarr_exclude_unmonitored"] = "false",
                ["sonarr_exclude_unmonitored_series"] = "false",
                ["sonarr_exclude_unmonitored_episodes"] = "false"
            };

            foreach (var (key, expectedValue) in expectedSettings)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT value FROM settings WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);

                var actualValue = await command.ExecuteScalarAsync(
                    TestContext.Current.CancellationToken);

                Assert.Equal(expectedValue, actualValue);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Sqlite_UnmonitoredSettingsMigrationPreservesExistingValues()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr_test_{Guid.NewGuid()}.db");
        try
        {
            var connectionString = $"Data Source={dbPath}";
            var services = new ServiceCollection();
            services.AddFluentMigrator(connectionString, "sqlite");

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp(21);

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO settings (key, value) VALUES
                    ('radarr_exclude_unmonitored', 'true'),
                    ('sonarr_exclude_unmonitored_series', 'true'),
                    ('sonarr_exclude_unmonitored_episodes', 'true')
                    """;

                await command.ExecuteNonQueryAsync(
                    TestContext.Current.CancellationToken);
            }

            runner.MigrateUp(22);

            await using var verifyConnection = new SqliteConnection(connectionString);
            await verifyConnection.OpenAsync(TestContext.Current.CancellationToken);

            foreach (var key in new[]
            {
                "radarr_exclude_unmonitored",
                "sonarr_exclude_unmonitored_series",
                "sonarr_exclude_unmonitored_episodes"
            })
            {
                await using var command = verifyConnection.CreateCommand();
                command.CommandText = "SELECT value FROM settings WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);

                var actualValue = await command.ExecuteScalarAsync(
                    TestContext.Current.CancellationToken);

                Assert.Equal("true", actualValue);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task MySql_MigrationsRunSuccessfully()
    {
        await using var container = new MySqlBuilder("mysql:latest")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = container.GetConnectionString();
        RunMigrations(connectionString, "mysql");

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task Postgres_MigrationsRunSuccessfully()
    {
        await using var container = new PostgreSqlBuilder("postgres:latest")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = container.GetConnectionString();
        RunMigrations(connectionString, "postgres");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }
}
