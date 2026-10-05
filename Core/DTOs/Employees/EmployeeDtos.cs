namespace Core.DTOs.Employees;
public sealed record EmployeeSummaryDto(
    Guid Id,
    string FullName,
    string PositionTitle);