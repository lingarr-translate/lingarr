using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Configuration;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder
            .HasOne(i => i.Movie)
            .WithMany(m => m.Images)
            .HasForeignKey(i => i.MovieId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(i => i.Show)
            .WithMany(s => s.Images)
            .HasForeignKey(i => i.ShowId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(i => i.MovieId);
        builder.HasIndex(i => i.ShowId);
    }
}