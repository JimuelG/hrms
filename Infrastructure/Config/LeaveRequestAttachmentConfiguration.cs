using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class LeaveRequestAttachmentConfiguration : IEntityTypeConfiguration<LeaveRequestAttachment>
{
    public void Configure(EntityTypeBuilder<LeaveRequestAttachment> builder)
    {
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();

        builder.HasOne<LeaveRequest>().WithMany().HasForeignKey(a => a.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.LeaveRequestId);
    }
}