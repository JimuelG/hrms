using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.Property(r => r.SubjectType).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Details).HasMaxLength(500);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.RequesterEmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Steps).WithOne(s => s.Request).HasForeignKey(s => s.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Events).WithOne(e => e.Request).HasForeignKey(e => e.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.SubjectType, r.SubjectId }).IsUnique();
        builder.HasIndex(r => new { r.RequesterEmployeeId, r.Status });
    }
}