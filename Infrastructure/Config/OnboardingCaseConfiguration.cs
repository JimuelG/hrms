using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class OnboardingCaseConfiguration : IEntityTypeConfiguration<OnboardingCase>
{
    public void Configure(EntityTypeBuilder<OnboardingCase> builder)
    {
        builder.HasOne(c => c.Application).WithMany().HasForeignKey(c => c.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.ApplicationId).IsUnique();
    }
}