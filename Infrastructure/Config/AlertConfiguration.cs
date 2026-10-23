using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.Property(a => a.Kind).HasConversion<int>();
        builder.Property(a => a.DedupeKey).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Title).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Message).HasMaxLength(500).IsRequired();

        builder.HasIndex(a => new { a.TenantId, a.DedupeKey }).IsUnique().HasFilter("[ResolvedAtUtc] IS NULL");
        builder.HasIndex(a => new { a.Kind, a.ResolvedAtUtc });
    }
}