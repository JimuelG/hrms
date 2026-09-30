using Core.Entities;

namespace Infrastructure.Identity;
public class UserTenant
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}