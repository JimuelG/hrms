using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(150).IsRequired();

        builder.HasOne(l => l.Branch).WithMany().HasForeignKey(l => l.BranchId)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}