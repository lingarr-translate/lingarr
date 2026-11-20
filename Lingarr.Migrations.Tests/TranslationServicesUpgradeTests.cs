using System.Data.Common;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lingarr.Migrations.Tests;

[Trait("Category", "Integration")]
public class TranslationServicesUpgradeTests : IDisposable
{
    private readonly List<string> _files = [];

    [Fact]
    public async Task Sqlite_MovesTheCascadeIntoTranslationServices()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        _files.Add(path);
        var connectionString = $"Data Source={path}";

        await Upgrade("sqlite", connectionString, "\"", () => new SqliteConnection(connectionString));
    }

    [Fact]
    public async Task MySql_MovesTheCascadeIntoTranslationServices()
    {
        var token = TestContext.Current.CancellationToken;
        await using var container = new MySqlBuilder("mysql:latest").Build();
        await container.StartAsync(token);
        var connectionString = container.GetConnectionString();

        await Upgrade("mysql", connectionString, "`", () => new MySqlConnection(connectionString));
    }

    [Fact]
    public async Task Postgres_MovesTheCascadeIntoTranslationServices()
    {
        var token = TestContext.Current.CancellationToken;
        await using var container = new PostgreSqlBuilder("postgres:latest").Build();
        await container.StartAsync(token);
        var connectionString = container.GetConnectionString();

        await Upgrade("postgresql", connectionString, "\"", () => new NpgsqlConnection(connectionString));
    }

    [Fact]
    public async Task Sqlite_CopyStoppedHalfway_RunsTheCopyAgain()
    {
        var token = TestContext.Current.CancellationToken;
        var connectionString = CreateSqliteDatabase();

        Migrate(connectionString, "sqlite", 23, null);
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync(token);
            await CreateTranslationServiceTables(connection);
            await Execute(connection,
                "UPDATE settings SET \"value\" = '[\"deepl\",\"openai\"]' WHERE \"key\" = 'service_type'");
            await Execute(connection,
                "UPDATE settings SET \"value\" = 'deepl-key' WHERE \"key\" = 'deepl_api_key'");
            await Execute(connection,
                "INSERT INTO translation_services (provider, name, \"position\", created_at, updated_at) " +
                "VALUES ('deepl', 'DeepL', 0, '2026-01-01', '2026-01-01')");
        }

        Migrate(connectionString, "sqlite", null, null);
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync(token);

            Assert.Equal(
                ["deepl|0", "openai|1"],
                await ReadRows(connection,
                    "SELECT provider, \"position\" FROM translation_services ORDER BY \"position\""));
            Assert.Equal(
                ["deepl|deepl_api_key|deepl-key"],
                await ReadRows(connection,
                    "SELECT service.provider, setting.\"key\", setting.\"value\" " +
                    "FROM translation_service_settings setting " +
                    "JOIN translation_services service ON service.id = setting.translation_service_id " +
                    "WHERE setting.\"key\" = 'deepl_api_key'"));
            Assert.Empty(await ReadRows(connection,
                "SELECT \"key\" FROM settings WHERE \"key\" IN ('service_type', 'deepl_api_key')"));
        }
    }

    [Fact]
    public async Task Sqlite_DeleteStoppedHalfway_KeepsTheCopyAndFinishesTheDelete()
    {
        var token = TestContext.Current.CancellationToken;
        var connectionString = CreateSqliteDatabase();

        Migrate(connectionString, "sqlite", 23, null);
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync(token);
            await CreateTranslationServiceTables(connection);
            await Execute(connection,
                "INSERT INTO translation_services (provider, name, \"position\", created_at, updated_at) " +
                "VALUES ('openai', 'OpenAI', 0, '2026-01-01', '2026-01-01')");
            await Execute(connection,
                "INSERT INTO translation_service_settings (translation_service_id, \"key\", \"value\") " +
                "VALUES (1, 'openai_api_key', 'copied-key')");
            await Execute(connection, "DELETE FROM settings WHERE \"key\" = 'service_type'");
        }

        Migrate(connectionString, "sqlite", null, null);
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync(token);

            Assert.Equal(
                ["openai|openai_api_key|copied-key"],
                await ReadRows(connection,
                    "SELECT service.provider, setting.\"key\", setting.\"value\" " +
                    "FROM translation_service_settings setting " +
                    "JOIN translation_services service ON service.id = setting.translation_service_id"));
            Assert.Empty(await ReadRows(connection,
                "SELECT \"key\" FROM settings WHERE \"key\" IN ('openai_api_key', 'deepl_api_key')"));
        }
    }

    private string CreateSqliteDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        _files.Add(path);
        return $"Data Source={path}";
    }

    private static async Task CreateTranslationServiceTables(DbConnection connection)
    {
        await Execute(connection,
            "CREATE TABLE translation_services (id INTEGER PRIMARY KEY AUTOINCREMENT, provider TEXT NOT NULL, " +
            "name TEXT NOT NULL, \"position\" INTEGER NOT NULL, created_at DATETIME NOT NULL, " +
            "updated_at DATETIME NOT NULL)");
        await Execute(connection,
            "CREATE TABLE translation_service_settings (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "translation_service_id INTEGER NOT NULL REFERENCES translation_services(id) ON DELETE CASCADE, " +
            "\"key\" TEXT NOT NULL, \"value\" TEXT NOT NULL)");
    }

    private static async Task Upgrade(
        string provider,
        string connectionString,
        string quote,
        Func<DbConnection> createConnection)
    {
        var token = TestContext.Current.CancellationToken;
        var key = quote + "key" + quote;
        var value = quote + "value" + quote;
        var position = quote + "position" + quote;

        Migrate(connectionString, provider, 23, null);
        await using (var connection = createConnection())
        {
            await connection.OpenAsync(token);
            await Execute(connection,
                $"UPDATE settings SET {value} = '[\"OpenAI\",\"localai\",\"openai\",\"cloudflare\"]' WHERE {key} = 'service_type'");
            await Execute(connection,
                $"UPDATE settings SET {value} = 'encrypted-key' WHERE {key} = 'openai_api_key'");
            await Execute(connection,
                $"UPDATE settings SET {value} = 'http://ollama:11434' WHERE {key} = 'local_ai_endpoint'");
            await Execute(connection,
                $"UPDATE settings SET {value} = 'deepl-key' WHERE {key} = 'deepl_api_key'");
            await Execute(connection,
                $"INSERT INTO settings ({key}, {value}, provider) VALUES ('cloudflare_account_id', 'account', 'Cloudflare')");
        }

        Migrate(connectionString, provider, null, null);
        await using (var connection = createConnection())
        {
            await connection.OpenAsync(token);

            Assert.Equal(
                ["openai|OpenAI|0", "localai|LocalAI / Ollama|1", "cloudflare|cloudflare|2"],
                await ReadRows(connection,
                    $"SELECT provider, name, {position} FROM translation_services ORDER BY {position}"));
            Assert.Equal(
                ["cloudflare|cloudflare_account_id|account", "openai|openai_api_key|encrypted-key"],
                await ReadRows(connection,
                    $"SELECT service.provider, setting.{key}, setting.{value} " +
                    "FROM translation_service_settings setting " +
                    "JOIN translation_services service ON service.id = setting.translation_service_id " +
                    $"WHERE setting.{key} IN ('openai_api_key', 'cloudflare_account_id') " +
                    $"ORDER BY setting.{key}"));
            Assert.Equal(
                ["localai|local_ai_endpoint|http://ollama:11434"],
                await ReadRows(connection,
                    $"SELECT service.provider, setting.{key}, setting.{value} " +
                    "FROM translation_service_settings setting " +
                    "JOIN translation_services service ON service.id = setting.translation_service_id " +
                    $"WHERE setting.{key} = 'local_ai_endpoint'"));
            Assert.Empty(await ReadRows(connection,
                $"SELECT {key} FROM settings WHERE {key} IN ('service_type', 'openai_api_key', 'deepl_api_key') " +
                "OR provider IS NOT NULL"));
        }

        Migrate(connectionString, provider, null, 25);
        await using (var connection = createConnection())
        {
            await connection.OpenAsync(token);

            Assert.Equal(
                [
                    "cloudflare_account_id|account|cloudflare",
                    "deepl_api_key||",
                    "local_ai_endpoint|http://ollama:11434|",
                    "local_ai_structured_output|true|",
                    "openai_api_key|encrypted-key|",
                    "service_type|[\"openai\",\"localai\",\"cloudflare\"]|"
                ],
                await ReadRows(connection,
                    $"SELECT {key}, {value}, provider FROM settings WHERE {key} IN " +
                    "('cloudflare_account_id', 'deepl_api_key', 'local_ai_endpoint', " +
                    "'local_ai_structured_output', 'openai_api_key', 'service_type') " +
                    $"ORDER BY {key}"));
        }
    }

    private static async Task Execute(DbConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<string>> ReadRows(DbConnection connection, string commandText)
    {
        var rows = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var values = new List<string>();
            for (var index = 0; index < reader.FieldCount; index++)
            {
                values.Add(reader.IsDBNull(index) ? "" : Convert.ToString(reader.GetValue(index))!);
            }

            rows.Add(string.Join("|", values));
        }

        return rows;
    }

    private static void Migrate(string connectionString, string provider, long? upVersion, long? downVersion)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(connectionString, provider);
        using var scope = services.BuildServiceProvider().CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (downVersion.HasValue)
        {
            runner.MigrateDown(downVersion.Value);
        }
        else if (upVersion.HasValue)
        {
            runner.MigrateUp(upVersion.Value);
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
