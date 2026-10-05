using Core.Interfaces;

namespace Core.Entities;
public class Position : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsManagerial { get; set; }
    public bool IsActive { get; set; } = true;
}