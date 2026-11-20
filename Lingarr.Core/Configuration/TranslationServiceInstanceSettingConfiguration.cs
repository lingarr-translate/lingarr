using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lingarr.Core.Entities;

namespace Lingarr.Core.Configuration;

public class TranslationServiceInstanceSettingConfiguration : IEntityTypeConfiguration<TranslationServiceInstanceSetting>
{
    public void Configure(EntityTypeBuilder<TranslationServiceInstanceSetting> builder)
    {
        builder.ToTable("translation_service_settings");

        builder
            .Property(setting => setting.TranslationServiceInstanceId)
            .HasColumnName("translation_service_id");

        builder
            .HasIndex(setting => new { setting.TranslationServiceInstanceId, setting.Key })
            .IsUnique();
    }
}
