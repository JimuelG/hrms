using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.HasIndex(p => p.Code).IsUnique();
    }
}