using Microsoft.EntityFrameworkCore;

namespace Lingarr.Core.Configuration;

public static class DatabaseConfiguration
{
    private static readonly string[] SupportedConnections = ["mysql", "postgres", "postgresql", "sqlite"];

    /// <summary>
    /// Gets the current database connection type from environment.
    /// Throws when unset or set to an unsupported value.
    /// </summary>
    public static string GetDbConnection()
    {
        var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION")?.ToLower();
        if (string.IsNullOrEmpty(dbConnection))
        {
            throw new InvalidOperationException(
                $"DB_CONNECTION is not set. Set it to one of: {string.Join(", ", SupportedConnections)}.");
        }

        if (!SupportedConnections.Contains(dbConnection))
        {
            throw UnsupportedConnection(dbConnection);
        }

        return dbConnection;
    }

    /// <summary>
    /// Gets the connection string for the current database configuration.
    /// </summary>
    public static string GetConnectionString(string? dbConnection = null)
    {
        dbConnection ??= GetDbConnection();

        return dbConnection switch
        {
            "mysql" => GetMySqlConnectionString(),
            "postgres" or "postgresql" => GetPostgresConnectionString(),
            "sqlite" => GetSqliteConnectionString(),
            _ => throw UnsupportedConnection(dbConnection)
        };
    }

    public static void ConfigureDbContext(DbContextOptionsBuilder options, string? dbConnection = null)
    {
        dbConnection ??= GetDbConnection();

        switch (dbConnection)
        {
            case "mysql":
                ConfigureMySql(options);
                break;
            case "postgres":
            case "postgresql":
                ConfigurePostgres(options);
                break;
            case "sqlite":
                ConfigureSqlite(options);
                break;
            default:
                throw UnsupportedConnection(dbConnection);
        }
    }

    private static InvalidOperationException UnsupportedConnection(string dbConnection)
    {
        return new InvalidOperationException(
            $"Database connection '{dbConnection}' is not supported. Set DB_CONNECTION to one of: {string.Join(", ", SupportedConnections)}.");
    }

    private static string GetMySqlConnectionString()
    {
        var variables = new Dictionary<string, string>
        {
            { "DB_HOST", Environment.GetEnvironmentVariable("DB_HOST") ?? "Lingarr.Mysql" },
            { "DB_PORT", Environment.GetEnvironmentVariable("DB_PORT") ?? "3306" },
            { "DB_DATABASE", Environment.GetEnvironmentVariable("DB_DATABASE") ?? "Lingarr" },
            { "DB_USERNAME", Environment.GetEnvironmentVariable("DB_USERNAME") ?? "Lingarr" },
            { "DB_PASSWORD", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Secret1234" }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            throw new InvalidOperationException(
                $"MySQL connection environment variable(s) '{string.Join(", ", missingVariables)}' is missing or empty.");
        }

        return $"Server={variables["DB_HOST"]};Port={variables["DB_PORT"]};Database={variables["DB_DATABASE"]};Uid={variables["DB_USERNAME"]};Pwd={variables["DB_PASSWORD"]};Allow User Variables=True";
    }

    private static string GetSqliteConnectionString()
    {
        var sqliteDbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "local.db";
        if (Path.IsPathRooted(sqliteDbPath))
        {
            return $"Data Source={sqliteDbPath};Foreign Keys=True";
        }

        return $"Data Source=/app/config/{sqliteDbPath};Foreign Keys=True";
    }

    private static string GetPostgresConnectionString()
    {
        var variables = new Dictionary<string, string>
        {
            { "DB_HOST", Environment.GetEnvironmentVariable("DB_HOST") ?? "Lingarr.Postgres" },
            { "DB_PORT", Environment.GetEnvironmentVariable("DB_PORT") ?? "5432" },
            { "DB_DATABASE", Environment.GetEnvironmentVariable("DB_DATABASE") ?? "Lingarr" },
            { "DB_USERNAME", Environment.GetEnvironmentVariable("DB_USERNAME") ?? "Lingarr" },
            { "DB_PASSWORD", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Secret1234" }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            throw new InvalidOperationException(
                $"PostgreSQL connection environment variable(s) '{string.Join(", ", missingVariables)}' is missing or empty.");
        }

        return $"Host={variables["DB_HOST"]};Port={variables["DB_PORT"]};Database={variables["DB_DATABASE"]};Username={variables["DB_USERNAME"]};Password={variables["DB_PASSWORD"]}";
    }

    /**
     * MySql configuration
     */
    private static void ConfigureMySql(DbContextOptionsBuilder options)
    {
        var connectionString = GetMySqlConnectionString();

        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysqlOptions => mysqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }

    /**
     * Sqlite configuration
     */
    private static void ConfigureSqlite(DbContextOptionsBuilder options)
    {
        var connectionString = GetSqliteConnectionString();

        options.UseSqlite(connectionString,
                sqliteOptions => sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .AddInterceptors(new SqlitePragmaConnectionInterceptor())
            .UseSnakeCaseNamingConvention();
    }

    /**
     * Postgres configuration
     */
    private static void ConfigurePostgres(DbContextOptionsBuilder options)
    {
        var connectionString = GetPostgresConnectionString();

        options.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }
}