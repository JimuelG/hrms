using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class WorkLocation : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = default!;
    public WorkLocationType Type { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? RadiusMeters { get; set; }

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public bool IsActive { get; set; }
}