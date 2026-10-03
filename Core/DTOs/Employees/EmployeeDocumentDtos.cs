using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Employees;

public sealed record EmployeeDocumentDto(
    Guid Id,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int Status,
    DateTime? VerifiedAtUtc,
    string? RejectionReason,
    DateOnly? ExpirationDate,
    DateTime UploadedAtUtc);

public sealed class VerifyDocumentDto
{
    [Required]
    public int Status { get; init; }
    [MaxLength(500)]
    public string? RejectionReason { get; init; }
}