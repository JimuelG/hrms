using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.Property(s => s.CompanyLegalName).HasMaxLength(200);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.Primarycolor).HasMaxLength(7);
        builder.Property(s => s.AddressLine).HasMaxLength(300);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.ContactEmail).HasMaxLength(256);
        builder.Property(s => s.ContactPhone).HasMaxLength(30);
        builder.Property(s => s.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.DateFormat).HasMaxLength(20).IsRequired();
        builder.Property(s => s.WorkingDays).HasConversion<int>();

        builder.HasIndex(s => s.TenantId).IsUnique();
    }
}