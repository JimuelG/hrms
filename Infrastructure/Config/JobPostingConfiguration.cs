using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(4000).IsRequired();
        builder.Property(j => j.Requirements).HasMaxLength(4000);
        builder.Property(j => j.Skills).HasMaxLength(1000);
        builder.Property(j => j.EducationRequirement).HasMaxLength(300);
        builder.Property(j => j.ExperienceRequirement).HasMaxLength(300);
        builder.Property(j => j.SalaryMin).HasColumnType("decimal(18,2)");
        builder.Property(j => j.SalaryMax).HasColumnType("decimal(18,2)");
    
        builder.HasOne(j => j.Position).WithMany().HasForeignKey(j => j.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(j => j.Department).WithMany().HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(j => j.Branch).WithMany().HasForeignKey(j => j.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(j => j.HiringManager).WithMany().HasForeignKey(j => j.HiringManagerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.Status);
    }
}