using Core.Interfaces;

namespace Core.Entities;
public class EmployeeEmergencyContact : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Relationship { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? AlternatePhone { get; set; }
    public string? Address { get; set; }
    public bool IsPrimary { get; set; }
}