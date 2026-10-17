using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.Property(r => r.StartPortion).HasConversion<int>();
        builder.Property(r => r.EndPortion).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Days).HasColumnType("decimal(6,1)");
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.CancellationReason).HasMaxLength(500);

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(r => r.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.EmployeeId, r.StartDate });
        builder.HasIndex(r => new { r.EmployeeId, r.LeaveTypeId, r.LeaveYear, r.Status });
        builder.HasIndex(r => r.Status);
    }
}