using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeEmergencyContactConfiguration : IEntityTypeConfiguration<EmployeeEmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmployeeEmergencyContact> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Relationship).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30).IsRequired();
        builder.Property(c => c.AlternatePhone).HasMaxLength(30);
        builder.Property(c => c.AlternatePhone).HasMaxLength(30);
        builder.Property(c => c.Address).HasMaxLength(300);

        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}