using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lingarr.Server.Tests.Configuration;

public class DatabaseConfigurationTests
{
    [Fact]
    public async Task ConfigureDbContext_Sqlite_EnablesWalAndBusyTimeout()
    {
        var originalSqliteDbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH");
        var dbPath = Path.Combine(Path.GetTempPath(), $"lingarr-db-{Guid.NewGuid():N}.db");

        try
        {
            Environment.SetEnvironmentVariable("SQLITE_DB_PATH", dbPath);

            var options = new DbContextOptionsBuilder<LingarrDbContext>();
            DatabaseConfiguration.ConfigureDbContext(options, "sqlite");

            await using var context = new LingarrDbContext(options.Options);
            await context.Database.OpenConnectionAsync();

            var connection = (SqliteConnection)context.Database.GetDbConnection();

            Assert.Equal("wal", await ExecuteScalarAsync(connection, "PRAGMA journal_mode;"));
            Assert.Equal(120000, await ExecuteScalarIntAsync(connection, "PRAGMA busy_timeout;"));
            Assert.Equal(1, await ExecuteScalarIntAsync(connection, "PRAGMA synchronous;"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQLITE_DB_PATH", originalSqliteDbPath);
            SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static async Task<object?> ExecuteScalarAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return await command.ExecuteScalarAsync();
    }

    private static async Task<int> ExecuteScalarIntAsync(SqliteConnection connection, string commandText)
    {
        var result = await ExecuteScalarAsync(connection, commandText);
        return Convert.ToInt32(result);
    }
}
