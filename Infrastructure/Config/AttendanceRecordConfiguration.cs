using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.Property(a => a.ClockInLocationName).HasMaxLength(150);
        builder.Property(a => a.ClockOutLocationName).HasMaxLength(150);
        builder.Property(a => a.Flags).HasConversion<int>();

        builder.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkLocation>().WithMany().HasForeignKey(a => a.ClockInWorkLocationId)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkLocation>().WithMany().HasForeignKey(a => a.ClockOutWorkLocationId)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
        builder.HasIndex(a => a.Date);
    }
}