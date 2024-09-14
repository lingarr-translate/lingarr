using Lingarr.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lingarr.Migrations.SQLite;

    public class SqliteDbContextFactory : IDesignTimeDbContextFactory<LingarrDbContext>
    {
        public LingarrDbContext CreateDbContext(string[] args)
        {
            var sqliteDbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "local.db";
            var optionsBuilder = new DbContextOptionsBuilder<LingarrDbContext>();
            optionsBuilder.UseSqlite($"Data Source=/app/config/{sqliteDbPath}",
                    sqliteOptions => sqliteOptions.MigrationsAssembly("Lingarr.Migrations"))
                .UseSnakeCaseNamingConvention();

            return new LingarrDbContext(optionsBuilder.Options);
        }
    }
