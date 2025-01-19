using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Configuration;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder
            .HasMany(m => m.Images)
            .WithOne(i => i.Movie)
            .HasForeignKey(i => i.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Navigation(m => m.Images)
            .AutoInclude();
    }
}