using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeCompensationConfiguration : IEntityTypeConfiguration<EmployeeCompensation>
{
    public void Configure(EntityTypeBuilder<EmployeeCompensation> builder)
    {
        builder.Property(c => c.MonthlyBasicSalary).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();

        builder.HasOne<Employee>().WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EmployeeId, c.EffectiveFrom }).IsUnique();
    }
}