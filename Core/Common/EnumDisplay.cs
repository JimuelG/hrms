using Core.Enums;

namespace Core.Common;

public static class EmployeeStatusExtensions
{
    public static string ToDisplayName(this EmployeeStatus status) => status switch
    {
        EmployeeStatus.Applicant => "Applicant",
        EmployeeStatus.PreOnboarding => "Pre-onboarding",
        EmployeeStatus.Probationary => "Probationary",
        EmployeeStatus.Regular => "Regular",
        EmployeeStatus.OnLeave => "On leave",
        EmployeeStatus.Suspended => "Suspended",
        EmployeeStatus.Resigned => "Resigned",
        EmployeeStatus.Terminated => "Terminated",
        EmployeeStatus.Archived => "Archived",
        _ => status.ToString()
    };
}

public static class EmploymentTypeExtensions
{
    public static string ToDisplayName(this EmploymentType type) => type switch
    {
        EmploymentType.FullTime => "Full-time",
        EmploymentType.PartTime => "Part-time",
        EmploymentType.Contractual => "Contractual",
        EmploymentType.Intern => "Intern",
        _ => type.ToString()
    };
}