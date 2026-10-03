using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.SubscriptionPlanId);
        builder.HasIndex(s => s.TenantId).IsUnique();
    }
}