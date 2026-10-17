using Core.Interfaces;

namespace Core.Entities;
public class LeaveRequestAttachment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid LeaveRequestId { get; set; }
    public string StorageKey { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
}