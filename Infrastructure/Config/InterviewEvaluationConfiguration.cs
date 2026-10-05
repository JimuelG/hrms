using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class InterviewEvaluationConfiguration : IEntityTypeConfiguration<InterviewEvaluation>
{
    public void Configure(EntityTypeBuilder<InterviewEvaluation> builder)
    {
        builder.Property(e => e.Strenghts).HasMaxLength(2000);
        builder.Property(e =>e.Concerns).HasMaxLength(2000);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.HasIndex(e => e.InterviewId).IsUnique();
    }
}