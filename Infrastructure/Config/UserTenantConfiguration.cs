using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        builder.HasKey(ut => new { ut.UserId, ut.TenantId });
        builder.HasOne(ut => ut.User).WithMany(u => u.Tenants).HasForeignKey(ut => ut.UserId);
        builder.HasOne(ut => ut.Tenant).WithMany().HasForeignKey(ut => ut.TenantId);
    }
}