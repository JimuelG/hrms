using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class Employee : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EmployeeNumber { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public Guid PositionId { get; set; }
    public Position Position { get; set; } = default!;

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    public Guid? ScheduleId { get; set; }
    public WorkSchedule? Schedule { get; set; }

    public EmploymentType EmploymentType { get; set; }
    public DateOnly HireDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Probationary;
}