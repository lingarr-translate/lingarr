using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Configuration;

public class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder
            .HasMany(s => s.Seasons)
            .WithOne(s => s.Show)
            .HasForeignKey(s => s.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(s => s.Images)
            .WithOne(i => i.Show)
            .HasForeignKey(i => i.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Seasons).AutoInclude();
    }
}