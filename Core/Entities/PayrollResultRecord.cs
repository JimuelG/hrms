using Core.Interfaces;

namespace Core.Entities;
public class PayrollResultRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun Run { get; set; } = default!;

    public Guid EmployeeId { get; set; }

    public string EmployeeNumber { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public string? DepartmentName { get; set; }
    public string? PositionTitle { get; set; }

    public decimal MonthlyBasic { get; set; }
    public decimal Gross { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal Net { get; set; }
    public decimal EmployerContributions { get; set; }

    public string LinesJson { get; set; } = "[]";
    public string NotesJson { get; set; } = "[]";
    public string WarningsJson { get; set; } = "[]";

    public string InputsJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }

}