using Core.Common;
using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class PendingApprovalsSpecification : BaseSpecfication<ApprovalRequest>
{
    public PendingApprovalsSpecification() : base(r => r.Status == ApprovalStatus.Pending) { }
}

public sealed class DocumentsExpiringBySpecification : BaseSpecfication<EmployeeDocument>
{
    public DocumentsExpiringBySpecification(DateOnly through) : base(d => d.ExpirationDate != null && d.ExpirationDate <= through) { }
}

public sealed class OpenAttendanceBeforeSpecification : BaseSpecfication<AttendanceRecord>
{
    public OpenAttendanceBeforeSpecification(DateOnly before) : base(a => a.ClockOutUtc == null && a.Date <= before) { }
}

public sealed class OpenAlertsSpecification : BaseSpecfication<Alert>
{
    public OpenAlertsSpecification(AlertKind? kind) : base(a => a.ResolvedAtUtc == null && (kind == null || a.Kind == kind))
    {
        AddOrderByDescending(a => a.CreatedAtUtc);
    }
}