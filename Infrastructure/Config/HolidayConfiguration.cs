using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.Property(h => h.Name).HasMaxLength(150).IsRequired();
        builder.Property(h => h.Type).HasConversion<int>();

        builder.HasIndex(h => new { h.TenantId, h.Date}).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}