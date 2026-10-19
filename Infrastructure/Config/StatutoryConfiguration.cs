using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class StatutoryConfiguration : IEntityTypeConfiguration<StatutoryRuleSet>
{
    public void Configure(EntityTypeBuilder<StatutoryRuleSet> builder)
    {
        builder.Property(r => r.Kind).HasConversion<int>();
        builder.Property(r => r.Method).HasConversion<int>();
        builder.Property(r => r.EmployeeRate).HasColumnType("decimal(9,6)");
        builder.Property(r => r.EmployerRate).HasColumnType("decimal(9,6)");
        builder.Property(r => r.BaseFloor).HasColumnType("decimal(18,2)");
        builder.Property(r => r.BaseCeiling).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Source).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasMany(r => r.Brackets).WithOne(b => b.StatutoryRuleSet).HasForeignKey(b => b.StatutoryRuleSetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.Kind, r.Basis, r.EffectiveFrom }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class StatutoryBrackConfiguration : IEntityTypeConfiguration<StatutoryBracket>
{
    public void Configure(EntityTypeBuilder<StatutoryBracket> builder)
    {
        builder.Property(b => b.FromAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.ToAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.EmployeeFixed).HasColumnType("decimal(18,2)");
        builder.Property(b => b.EmployerFixed).HasColumnType("decimal(18,2)");
        builder.Property(b => b.EmployeeRateOnExcess).HasColumnType("decimal(9,6)");

        builder.HasIndex(b => new { b.StatutoryRuleSetId, b.Order }).IsUnique();
    }
}