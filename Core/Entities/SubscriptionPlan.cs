using Core.Enums;

namespace Core.Entities;
public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int MaxEmployees { get; set; }
    public int MaxBranches { get; set; }
    public int MaxDepartments { get; set; }
    public int MaxHrUsers { get; set; }
    public PlanFeature Features { get; set; }
    public bool IsActive { get; set; } = true;
}