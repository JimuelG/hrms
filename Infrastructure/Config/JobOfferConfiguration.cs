using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class JobOfferConfiguration : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.Property(o => o.ProposedSalary).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Terms).HasMaxLength(2000);
        builder.Property(o => o.DeclineReason).HasMaxLength(500);

        builder.HasOne(o => o.Application).WithMany().HasForeignKey(o => o.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => o.ApplicationId);
        builder.HasIndex(o => o.Status);
    }
}