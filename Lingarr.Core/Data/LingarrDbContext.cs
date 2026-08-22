using Lingarr.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Data;

public class LingarrDbContext : DbContext
{
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Show> Shows { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<TranslationRequest> TranslationRequests { get; set; }
    public DbSet<TranslationRequestEvent> TranslationRequestEvents { get; set; }
    public DbSet<TranslationRequestLine> TranslationRequestLines { get; set; }
    public DbSet<PathMapping> PathMappings { get; set; }
    public DbSet<Statistics> Statistics { get; set; }
    public DbSet<DailyStatistics> DailyStatistics { get; set; }
    public DbSet<User> Users { get; set; }

    public LingarrDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MovieConfiguration());
        modelBuilder.ApplyConfiguration(new ShowConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new EpisodeConfiguration());
        modelBuilder.ApplyConfiguration(new ImageConfiguration());

        ConvertTimestampsToUtc(modelBuilder, Database.IsNpgsql());
    }

    private static void ConvertTimestampsToUtc(ModelBuilder modelBuilder, bool isNpgsql)
    {
        var converter = new ValueConverter<DateTimeOffset, DateTime>(
            value => DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified),
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        var nullableConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value.UtcDateTime, DateTimeKind.Unspecified)
                : null,
            value => value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType != typeof(DateTimeOffset) && property.ClrType != typeof(DateTimeOffset?))
                {
                    continue;
                }

                if (isNpgsql)
                {
                    property.SetColumnType("timestamp without time zone");
                }

                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(converter);
                }
                else
                {
                    property.SetValueConverter(nullableConverter);
                }
            }
        }
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
                ((BaseEntity)entity.Entity).CreatedAt = DateTimeOffset.UtcNow;
            }

            ((BaseEntity)entity.Entity).UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}