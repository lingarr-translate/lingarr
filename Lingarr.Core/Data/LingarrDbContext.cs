using Microsoft.EntityFrameworkCore;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Data;

public class LingarrDbContext : DbContext
{
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Show> Shows { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<Media> Media { get; set; }
    public DbSet<Setting> Settings { get; set; }

   public LingarrDbContext(DbContextOptions options) : base(options)
   {
   }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

        foreach (var entity in entities)
        {
            if (entity.State == EntityState.Added)
            {
                ((BaseEntity)entity.Entity).CreatedAt = DateTime.UtcNow;
            }

            ((BaseEntity)entity.Entity).UpdatedAt = DateTime.UtcNow;
        }
    }
}