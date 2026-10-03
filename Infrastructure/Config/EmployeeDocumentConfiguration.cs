using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(400).IsRequired();
        builder.Property(d => d.RejectionReason).HasMaxLength(500);

        builder.HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => d.EmployeeId);
    }
}