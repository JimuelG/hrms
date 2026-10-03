using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class EmployeeTimelineEventConfiguration : IEntityTypeConfiguration<EmployeeTimelineEvent>
{
    public void Configure(EntityTypeBuilder<EmployeeTimelineEvent> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.EmployeeId, e.EventDateUtc });
    }
}