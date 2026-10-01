using Core.Interfaces;

namespace Core.Entities;
public class Department : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}