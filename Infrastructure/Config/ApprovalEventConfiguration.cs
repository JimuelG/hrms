using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ApprovalEventConfiguration : IEntityTypeConfiguration<ApprovalEvent>
{
    public void Configure(EntityTypeBuilder<ApprovalEvent> builder)
    {
        builder.Property(e => e.Type).HasConversion<int>();
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.HasIndex(e => e.ApprovalRequestId);
    }
}