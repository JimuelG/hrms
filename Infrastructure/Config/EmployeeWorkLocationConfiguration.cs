using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeWorkLocationConfiguration : IEntityTypeConfiguration<EmployeeWorkLocation>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkLocation> builder)
    {
        builder.HasOne<Employee>().WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkLocation>().WithMany().HasForeignKey(a => a.WorkLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.EmployeeId, a.WorkLocationId }).IsUnique();
    }
}