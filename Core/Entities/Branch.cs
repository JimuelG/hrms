using Core.Interfaces;

namespace Core.Entities;
public class Branch : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? TimeZoneId { get; set; }
    public bool IsActive { get; set; } = true;
}