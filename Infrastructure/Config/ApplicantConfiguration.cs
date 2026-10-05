using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ApplicantConfiguration : IEntityTypeConfiguration<Applicant>
{
    public void Configure(EntityTypeBuilder<Applicant> builder)
    {
        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(30);
        builder.Property(a => a.SkillsSumarry).HasMaxLength(1000);
        builder.Property(a => a.EducationSummary).HasMaxLength(1000);
        builder.Property(a => a.ExperienceSummary).HasMaxLength(1000);
        builder.Property(a => a.ResumeOriginalFileName).HasMaxLength(260);
        builder.Property(a => a.ResumeContentType).HasMaxLength(100);
        builder.Property(a => a.ResumeStorageKey).HasMaxLength(400);

        builder.HasIndex(a => new { a.TenantId, a.Email }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}