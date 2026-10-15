using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class LeaveBalanceEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }

    public int Year { get; set; }
    public LeaveEntryType EntryType { get; set; }
    public decimal Days { get; set; }
    public string Description { get; set; } = default!;
    
    public bool IsAutomatic { get; set; }
    public Guid? CreatedByUserId { get; set; }
}