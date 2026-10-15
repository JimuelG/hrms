using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class Holiday : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = default!;
    public HolidayType Type { get; set; } = HolidayType.Public;
}