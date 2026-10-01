using Core.Interfaces;

namespace Core.Entities;
public class Role : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsSystemRole { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission : ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

public class UserTenantRole
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
}