using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.WorkingDays).HasConversion<int>();
        builder.HasIndex(s => new { s.TenantId, s.Name}).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}