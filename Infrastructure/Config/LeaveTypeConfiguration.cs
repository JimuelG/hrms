using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.EntitlementMode).HasConversion<int>();
        builder.Property(t => t.DefaultDays).HasColumnType("decimal(6, 1)");
        builder.Property(t => t.MaxCarryOverDays).HasColumnType("decimal(6, 1)");

        builder.HasIndex(t => new { t.TenantId, t.Code}).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}