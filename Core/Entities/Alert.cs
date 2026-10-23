using Core.Common;
using Core.Interfaces;

namespace Core.Entities;
public class Alert : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public AlertKind Kind { get; set; }
    public string DedupeKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public Guid? SubjectId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}