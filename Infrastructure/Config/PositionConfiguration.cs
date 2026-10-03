using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}