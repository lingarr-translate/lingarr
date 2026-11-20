using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Configuration;

public class TranslationServiceInstanceConfiguration : IEntityTypeConfiguration<TranslationServiceInstance>
{
    public void Configure(EntityTypeBuilder<TranslationServiceInstance> builder)
    {
        builder.ToTable("translation_services");

        builder
            .HasMany(instance => instance.Settings)
            .WithOne(setting => setting.TranslationServiceInstance)
            .HasForeignKey(setting => setting.TranslationServiceInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
