using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Organization;
public sealed record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    DateOnly? DateOfBirth,
    Guid BranchId,
    string BranchName,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionTitle,
    Guid? ManagerId,
    string? ManagerName,
    int EmploymentType,
    DateOnly HireDate,
    int Status,
    DateTime CreatedAtUtc);

public sealed class CreateEmployeeDto
{
    [Required, MaxLength(30)]
    public string EmployeeNumber { get; init; } = "";
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";
    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";
    [MaxLength(30)]
    public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    [Required]
    public Guid BranchId { get; init; }
    [Required]
    public Guid DepartmentId { get; init; }
    [Required]
    public Guid PositionId { get; init; }
    public Guid? ManagerId { get; init; }

    [Required]
    public int EmploymentType { get; init; }
    [Required]
    public DateOnly HireDate { get; init; }

    public int Status { get; init; }
}

public sealed class UpdateEmployeeDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";
    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";
    [MaxLength(30)]
    public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    [Required]
    public Guid BranchId { get; init; }
    [Required]
    public Guid DepartmentId { get; init; }
    [Required]
    public Guid PositionId { get; init; }
    public Guid? ManagerId { get; init; }

    [Required]
    public int EmploymentType { get; init; }
    [Required]
    public DateOnly HireDate { get; init; }
    [Required]
    public int Status { get; init; }
}

public sealed record EmergencyContactDto(
    Guid Id,
    string Name,
    string Relationship,
    string Phone,
    string? AlternatePhone,
    string? Address,
    bool IsPrimary
);

public sealed class UpsertEmergencyContactDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(50)]
    public string Relationship { get; init; } = "";
    [Required, MaxLength(30)]
    public string Phone { get; init; } = "";
    [MaxLength(30)]
    public string? AlternatePhone { get; init; }
    [MaxLength(300)]
    public string? Address { get; init; }
    public bool IsPrimary { get; init; }
}