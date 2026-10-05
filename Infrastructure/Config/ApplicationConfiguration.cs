using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ApplicationConfiguration : IEntityTypeConfiguration<Core.Entities.Application>
{
    public void Configure(EntityTypeBuilder<Core.Entities.Application> builder)
    {
        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.HasOne(a => a.Applicant).WithMany(ap => ap.Applications).HasForeignKey(a => a.ApplicantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.JobPosting).WithMany().HasForeignKey(a => a.JobPostingId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ApplicantId, a.JobPostingId }).IsUnique();
        builder.HasIndex(a => a.Status);
    }
}