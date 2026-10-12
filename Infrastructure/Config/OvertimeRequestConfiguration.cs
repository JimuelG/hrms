using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ReviewNote).HasMaxLength(500);
        builder.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.EmployeeId, r.Date});
        builder.HasIndex(r => r.Status);
    }
}