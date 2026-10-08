using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class WorkSchedule : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = default!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public WorkingDay WorkingDays { get; set; } =
        WorkingDay.Monday |
        WorkingDay.Tuesday |
        WorkingDay.Wednesday |
        WorkingDay.Thursday |
        WorkingDay.Friday |
        WorkingDay.Saturday;
    
    public bool IsActive { get; set; } = true;
}