using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lingarr.Migrations.SQLite;

public class SqliteDbContextFactory : IDesignTimeDbContextFactory<LingarrDbContext>
{
    public LingarrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LingarrDbContext>();
        DatabaseConfiguration.ConfigureDbContext(optionsBuilder, "sqlite");
    
        return new LingarrDbContext(optionsBuilder.Options);
    }
}
