using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.Property(s => s.Kind).HasConversion<int>();
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.Label).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Permission).HasMaxLength(100);
        builder.Property(s => s.SkipReason).HasMaxLength(300);
        builder.Property(s => s.Note).HasMaxLength(500);

        builder.HasIndex(s => new { s.ApprovalRequestId, s.Order }).IsUnique();
        builder.HasIndex(s => new { s.Status, s.Kind });
    }
}