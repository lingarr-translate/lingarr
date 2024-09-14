using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Lingarr.Core.Data;

namespace Lingarr.Migrations.MySQL
{
    public class MySqlDbContextFactory : IDesignTimeDbContextFactory<LingarrDbContext>
    {
        public LingarrDbContext CreateDbContext(string[] args)
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

            var optionsBuilder = new DbContextOptionsBuilder<LingarrDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                    mysqlOptions => mysqlOptions.MigrationsAssembly("Lingarr.Migrations"))
                .UseSnakeCaseNamingConvention();

            return new LingarrDbContext(optionsBuilder.Options);
        }
    }
}