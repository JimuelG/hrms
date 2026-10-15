using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class LeaveBalanceEntryConfiguration : IEntityTypeConfiguration<LeaveBalanceEntry>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceEntry> builder)
    {
        builder.Property(e => e.EntryType).HasConversion<int>();
        builder.Property(e => e.Days).HasColumnType("decimal(6,1)");
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year });

        builder.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year, e.EntryType })
            .IsUnique()
            .HasFilter("[IsAutomatic] = 1");
    }
}