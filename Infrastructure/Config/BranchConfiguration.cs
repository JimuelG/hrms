using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.Property(b => b.AddressLine).HasMaxLength(300);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.Country).HasMaxLength(100);
        builder.Property(b => b.TimeZoneId).HasMaxLength(100);

        builder.HasIndex(b => new { b.TenantId, b.Code })
            .IsUnique().HasFilter("[IsDeleted] = 0");
    }
}