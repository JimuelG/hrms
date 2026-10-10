using Core.Interfaces;

namespace Core.Entities;
public class EmployeeWorkLocation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid WorkLocationId { get; set; }
}
