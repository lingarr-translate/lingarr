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
    public async Task MySql_MigrationsRunSuccessfully()
    {
        await using var container = new MySqlBuilder()
            .WithImage("mysql:latest")
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
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = container.GetConnectionString();
        RunMigrations(connectionString, "postgres");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }
}
