using System.Data.Common;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Lingarr.Migrations;

public static class MigrationConfiguration
{
    private const string VersionTableName = "version_info";
    private static readonly long[] InitialMigrationVersions = [1, 2];
    private static string? _connectionString;
    private static string? _dbConnection;

    /// <summary>
    /// Configures FluentMigrator services for the specified database provider.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="dbConnection">The database type: "mysql", "postgres", or "sqlite".</param>
    public static IServiceCollection AddFluentMigrator(
        this IServiceCollection services,
        string connectionString,
        string dbConnection)
    {
        _connectionString = connectionString;
        _dbConnection = dbConnection;

        services.AddFluentMigratorCore()
            .ConfigureRunner(migrationRunnerBuilder =>
            {
                switch (dbConnection.ToLowerInvariant())
                {
                    case "mysql":
                        migrationRunnerBuilder.AddMySql5();
                        break;
                    case "postgres":
                    case "postgresql":
                        migrationRunnerBuilder.AddPostgres();
                        break;
                    default:
                        migrationRunnerBuilder.AddSQLite();
                        break;
                }

                migrationRunnerBuilder.WithGlobalConnectionString(connectionString)
                    .WithVersionTable(new CustomVersionTableMetaData())
                    .ScanIn(typeof(MigrationConfiguration).Assembly).For.Migrations();
            })
            .AddLogging(migrationRunnerBuilder => migrationRunnerBuilder.AddFluentMigratorConsole());

        return services;
    }

    /// <summary>
    /// Runs all pending migrations, handling transition from EF Core migrations.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing the migration runner.</param>
    public static void RunMigrations(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        // Check if this is an existing database and handle transition
        HandleDatabaseTransition();

        // Run any pending migrations
        runner.MigrateUp();
    }

    /// <summary>
    /// Handles the transition from EF Core migrations by marking initial migrations as applied.
    /// </summary>
    private static void HandleDatabaseTransition()
    {
        if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_dbConnection))
        {
            return;
        }

        try
        {
            using var connection = CreateConnection(_dbConnection, _connectionString);
            connection.Open();

            if (!TableExists(connection, _dbConnection, "settings"))
            {
                return;
            }
            if (TableExists(connection, _dbConnection, VersionTableName))
            {
                if (MigrationRecordExists(connection, _dbConnection, 1))
                {
                    return;
                }
            }
            else
            {
                CreateVersionInfoTable(connection, _dbConnection);
            }

            foreach (var version in InitialMigrationVersions)
            {
                InsertVersionRecord(connection, _dbConnection, version);
            }

            DropEfCoreMigrationsHistory(connection, _dbConnection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not check for existing database: {ex.Message}");
        }
    }

    private static void DropEfCoreMigrationsHistory(DbConnection connection, string dbConnection)
    {
        var tableName = "__EFMigrationsHistory";

        if (!TableExists(connection, dbConnection, tableName))
        {
            return;
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = dbConnection.ToLowerInvariant() switch
            {
                "mysql" => $"DROP TABLE IF EXISTS `{tableName}`",
                "postgres" or "postgresql" => $"DROP TABLE IF EXISTS \"{tableName}\"",
                _ => $"DROP TABLE IF EXISTS \"{tableName}\""
            };

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not remove EF Core migrations history: {ex.Message}");
        }
    }

    private static DbConnection CreateConnection(string dbConnection, string connectionString)
    {
        return dbConnection.ToLowerInvariant() switch
        {
            "mysql" => new MySqlConnector.MySqlConnection(connectionString),
            "postgres" or "postgresql" => new Npgsql.NpgsqlConnection(connectionString),
            _ => new Microsoft.Data.Sqlite.SqliteConnection(connectionString)
        };
    }

    private static bool TableExists(DbConnection connection, string dbConnection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = dbConnection.ToLowerInvariant() switch
        {
            "sqlite" => $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'",
            "mysql" => $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'",
            "postgres" or "postgresql" => $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'",
            _ => $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'"
        };

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }

    private static bool MigrationRecordExists(DbConnection connection, string dbConnection, long version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = dbConnection.ToLowerInvariant() switch
        {
            "mysql" => $"SELECT COUNT(*) FROM {VersionTableName} WHERE version = {version}",
            _ => $"SELECT COUNT(*) FROM \"{VersionTableName}\" WHERE \"version\" = {version}"
        };

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }

    private static void CreateVersionInfoTable(DbConnection connection, string dbConnection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = dbConnection.ToLowerInvariant() switch
        {
            "mysql" => $@"CREATE TABLE IF NOT EXISTS {VersionTableName} (
                version BIGINT NOT NULL,
                applied_on DATETIME,
                description VARCHAR(1024)
            )",
            "postgres" or "postgresql" => $@"CREATE TABLE IF NOT EXISTS ""{VersionTableName}"" (
                ""version"" BIGINT NOT NULL,
                ""applied_on"" TIMESTAMP,
                ""description"" VARCHAR(1024)
            )",
            _ => $@"CREATE TABLE IF NOT EXISTS ""{VersionTableName}"" (
                ""version"" INTEGER NOT NULL,
                ""applied_on"" TEXT,
                ""description"" TEXT
            )"
        };

        command.ExecuteNonQuery();
    }

    private static void InsertVersionRecord(DbConnection connection, string dbConnection, long version)
    {
        using var command = connection.CreateCommand();
        var now = DateTime.UtcNow;
        var description = $"Migrated from EF Core (applied {now:yyyy-MM-dd HH:mm:ss})";

        command.CommandText = dbConnection.ToLowerInvariant() switch
        {
            "mysql" => $"INSERT INTO {VersionTableName} (version, applied_on, description) VALUES ({version}, '{now:yyyy-MM-dd HH:mm:ss}', '{description}')",
            "postgres" or "postgresql" => $"INSERT INTO \"{VersionTableName}\" (\"version\", \"applied_on\", \"description\") VALUES ({version}, '{now:yyyy-MM-dd HH:mm:ss}', '{description}')",
            _ => $"INSERT INTO \"{VersionTableName}\" (\"version\", \"applied_on\", \"description\") VALUES ({version}, '{now:yyyy-MM-dd HH:mm:ss}', '{description}')"
        };

        command.ExecuteNonQuery();
    }
}