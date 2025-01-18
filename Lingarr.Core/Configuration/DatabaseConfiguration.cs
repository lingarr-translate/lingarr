using Microsoft.EntityFrameworkCore;

namespace Lingarr.Core.Configuration;

public static class DatabaseConfiguration
{
    public static void ConfigureDbContext(DbContextOptionsBuilder options, string? dbConnection = null)
    {
        dbConnection ??= Environment.GetEnvironmentVariable("DB_CONNECTION")?.ToLower() ?? "sqlite";

        if (dbConnection == "mysql")
        {
            ConfigureMySql(options);
        }
        else
        {
            ConfigureSqlite(options);
        }
    }

    private static void ConfigureMySql(DbContextOptionsBuilder options)
    {
        var variables = new Dictionary<string, string>
        {
            { "DB_HOST", Environment.GetEnvironmentVariable("DB_HOST") ?? "Lingarr.Mysql" },
            { "DB_PORT", Environment.GetEnvironmentVariable("DB_PORT") ?? "3306" },
            { "DB_DATABASE", Environment.GetEnvironmentVariable("DB_DATABASE") ?? "LingarrMysql" },
            { "DB_USERNAME", Environment.GetEnvironmentVariable("DB_USERNAME") ?? "LingarrMysql" },
            { "DB_PASSWORD", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Secret1234" }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            throw new InvalidOperationException(
                $"MySQL connection environment variable(s) '{string.Join(", ", missingVariables)}' is missing or empty.");
        }

        var connectionString =
            $"Server={variables["DB_HOST"]};Port={variables["DB_PORT"]};Database={variables["DB_DATABASE"]};Uid={variables["DB_USERNAME"]};Pwd={variables["DB_PASSWORD"]};Allow User Variables=True";

        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysqlOptions => mysqlOptions.MigrationsAssembly("Lingarr.Migrations.MySQL")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }

    private static void ConfigureSqlite(DbContextOptionsBuilder options)
    {
        var sqliteDbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "local.db";
        options.UseSqlite($"Data Source=/app/config/{sqliteDbPath};Foreign Keys=True",
                sqliteOptions => sqliteOptions.MigrationsAssembly("Lingarr.Migrations.SQLite")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }
}