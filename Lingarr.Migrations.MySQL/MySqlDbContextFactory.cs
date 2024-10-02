using Lingarr.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Lingarr.Core.Data;

namespace Lingarr.Migrations.MySQL;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<LingarrDbContext>
{
    public LingarrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LingarrDbContext>();
        DatabaseConfiguration.ConfigureDbContext(optionsBuilder, "mysql");
        
        return new LingarrDbContext(optionsBuilder.Options);
    }
}
