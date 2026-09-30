using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public bool IsPlatformAdmin { get; set; }
    public ICollection<UserTenant> Tenants { get; set; } = [];
}