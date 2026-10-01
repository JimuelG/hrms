using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class UserTenantRoleConfiguration : IEntityTypeConfiguration<UserTenantRole>
{
    public void Configure(EntityTypeBuilder<UserTenantRole> builder)
    {
        builder.HasKey(utr => new { utr.UserId, utr.TenantId, utr.RoleId });
        builder.HasOne(utr => utr.Role).WithMany().HasForeignKey(utr => utr.RoleId).IsRequired(false);
    }
}