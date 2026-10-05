using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.Property(i => i.Location).HasMaxLength(500);
        builder.Property(i => i.CancellationReason).HasMaxLength(500);

        builder.HasOne(i => i.Application).WithMany().HasForeignKey(i => i.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Interviewer).WithMany().HasForeignKey(i => i.InterviewerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Evaluation).WithOne(e => e.Interview).HasForeignKey<InterviewEvaluation>(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ApplicationId);
        builder.HasIndex(i => new { i.InterviewerId, i.ScheduledAtUtc });
    }
}