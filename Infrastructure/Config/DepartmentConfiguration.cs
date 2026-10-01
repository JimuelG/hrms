using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.HasIndex(d => new { d.TenantId, d.Code })
            .IsUnique().HasFilter("[IsDeleted] = 0");
    }
}