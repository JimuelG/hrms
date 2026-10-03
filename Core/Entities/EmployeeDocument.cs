using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class EmployeeDocument : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public string DocumentType { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = default!;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}