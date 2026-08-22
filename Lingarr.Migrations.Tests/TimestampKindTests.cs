using Lingarr.Core.Data;
using Lingarr.Core.Entities;
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
public class TimestampKindTests
{
    private const string ServerTimeZone = "Europe/Amsterdam";
    private static readonly DateTimeOffset Written = new(2026, 8, 20, 20, 52, 17, TimeSpan.Zero);

    [Fact]
    public async Task Sqlite_ReadsBackAsUtc()
    {
        var connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db")}";
        try
        {
            RunMigrations(connectionString, "sqlite");

            await AssertRoundTrip(() => new DbContextOptionsBuilder<LingarrDbContext>()
                .UseSqlite(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task MySql_ReadsBackAsUtc()
    {
        var token = TestContext.Current.CancellationToken;

        await using var container = new MySqlBuilder("mysql:latest")
            .WithCommand("--default-time-zone=+02:00")
            .Build();
        await container.StartAsync(token);

        var connectionString = container.GetConnectionString();
        RunMigrations(connectionString, "mysql");

        await AssertRoundTrip(() => new DbContextOptionsBuilder<LingarrDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .UseSnakeCaseNamingConvention()
            .Options);

        MySqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task Postgres_ReadsBackAsUtc()
    {
        var token = TestContext.Current.CancellationToken;

        await using var container = new PostgreSqlBuilder("postgres:latest").Build();
        await container.StartAsync(token);

        var connectionString = container.GetConnectionString();
        await SetPostgresTimeZone(connectionString);
        RunMigrations(connectionString, "postgresql");

        await AssertRoundTrip(() => new DbContextOptionsBuilder<LingarrDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

        NpgsqlConnection.ClearAllPools();
    }

    private static async Task AssertRoundTrip(Func<DbContextOptions<LingarrDbContext>> buildOptions)
    {
        var token = TestContext.Current.CancellationToken;
        int id;

        await using (var context = new LingarrDbContext(buildOptions()))
        {
            var request = new TranslationRequest
            {
                Title = "Movie",
                MediaId = 1,
                MediaType = Core.Enum.MediaType.Movie,
                SourceLanguage = "en",
                TargetLanguage = "nl",
                Status = Core.Enum.TranslationStatus.Completed,
                CompletedAt = Written
            };
            context.TranslationRequests.Add(request);
            await context.SaveChangesAsync(token);
            id = request.Id;
        }

        await using (var context = new LingarrDbContext(buildOptions()))
        {
            var reloaded = await context.TranslationRequests.SingleAsync(row => row.Id == id, token);

            Assert.Equal(Written, reloaded.CompletedAt);
            Assert.Equal(TimeSpan.Zero, reloaded.CompletedAt!.Value.Offset);
            Assert.Equal(TimeSpan.Zero, reloaded.CreatedAt.Offset);
            Assert.Equal(TimeSpan.Zero, reloaded.UpdatedAt.Offset);

            Assert.Equal("\"2026-08-20T20:52:17+00:00\"",
                System.Text.Json.JsonSerializer.Serialize(reloaded.CompletedAt));

            await AssertQueriesTranslate(context);
        }

        await AssertExecuteUpdateRoundTrip(buildOptions, id);
    }

    /// <summary>
    /// ExecuteUpdate builds its statement without the change tracker, so the converter has to be
    /// proven on that path as well as on SaveChanges.
    /// </summary>
    private static async Task AssertExecuteUpdateRoundTrip(
        Func<DbContextOptions<LingarrDbContext>> buildOptions,
        int id)
    {
        var token = TestContext.Current.CancellationToken;
        var updated = Written.AddHours(3);

        await using (var context = new LingarrDbContext(buildOptions()))
        {
            await context.TranslationRequests
                .Where(row => row.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.CompletedAt, updated)
                    .SetProperty(row => row.UpdatedAt, updated), token);
        }

        await using (var context = new LingarrDbContext(buildOptions()))
        {
            var reloaded = await context.TranslationRequests.SingleAsync(row => row.Id == id, token);

            Assert.Equal(updated, reloaded.CompletedAt);
            Assert.Equal(TimeSpan.Zero, reloaded.CompletedAt!.Value.Offset);
            Assert.Equal(updated, reloaded.UpdatedAt);
            Assert.Equal(TimeSpan.Zero, reloaded.UpdatedAt.Offset);
        }
    }

    /// <summary>
    /// SQLite cannot order or compare DateTimeOffset without the converter, so every query shape
    /// Lingarr runs over a timestamp or a day has to be proven on each provider.
    /// </summary>
    private static async Task AssertQueriesTranslate(LingarrDbContext context)
    {
        var token = TestContext.Current.CancellationToken;
        var cutoff = Written.AddDays(-7);

        await context.TranslationRequests.Where(row => row.CreatedAt < cutoff).CountAsync(token);
        await context.TranslationRequests.Where(row => row.CompletedAt >= cutoff).CountAsync(token);
        await context.TranslationRequests.OrderBy(row => row.CreatedAt).FirstOrDefaultAsync(token);
        await context.TranslationRequests.OrderByDescending(row => row.CompletedAt).FirstOrDefaultAsync(token);
        await context.TranslationRequests.MaxAsync(row => (DateTimeOffset?)row.CreatedAt, token);
        await context.Movies.OrderBy(row => row.DateAdded).FirstOrDefaultAsync(token);
        await context.Movies.OrderByDescending(row => row.DateAdded).FirstOrDefaultAsync(token);
        await context.TranslationRequestEvents.OrderByDescending(row => row.CreatedAt).FirstOrDefaultAsync(token);
        await context.Users.Where(row => row.LastLoginAt >= cutoff).CountAsync(token);

        var day = DateOnly.FromDateTime(Written.UtcDateTime).AddDays(-30);
        await context.DailyStatistics.Where(row => row.Date >= day).OrderBy(row => row.Date).ToListAsync(token);
        await context.DailyStatistics.MaxAsync(row => (DateOnly?)row.Date, token);
    }

    private static async Task SetPostgresTimeZone(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE postgres SET TimeZone = '{ServerTimeZone}'";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        NpgsqlConnection.ClearPool(connection);
    }

    private static void RunMigrations(string connectionString, string provider)
    {
        var services = new ServiceCollection();
        services.AddFluentMigrator(connectionString, provider);

        MigrationConfiguration.RunMigrations(services.BuildServiceProvider());
    }
}
