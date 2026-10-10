using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(e => e.EmployeeNumber).HasMaxLength(30).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(30);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.TenantId, e.Email}).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique().HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(e => e.Manager).WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Schedule).WithMany().HasForeignKey(e => e.ScheduleId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    }
}