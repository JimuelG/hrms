using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class ApprovalRequestWithStepsSpecification : BaseSpecfication<ApprovalRequest>
{
    public ApprovalRequestWithStepsSpecification(Guid id) : base(r => r.Id == id)
    {
        AddInclude(r => r.Steps);
    }
}

public sealed class ApprovalRequestBySubjectSpecification : BaseSpecfication<ApprovalRequest>
{
    public ApprovalRequestBySubjectSpecification(ApprovalSubjectType type, Guid subjectId) : base(r => r.SubjectType == type && r.SubjectId == subjectId)
    {
        AddInclude(r => r.Steps);
    }
}

public sealed class ApprovalEventsByRequestSpecification : BaseSpecfication<ApprovalEvent>
{
    public ApprovalEventsByRequestSpecification(Guid requestId) : base(e => e.ApprovalRequestId == requestId)
    {
        AddOrderBy(e => e.CreatedAtUtc);
    }
}

public sealed class ApprovalRequestsByRequesterSpecification : BaseSpecfication<ApprovalRequest>
{
    public ApprovalRequestsByRequesterSpecification(Guid employeeId, ApprovalStatus? status, int? skip = null, int? take = null)
        : base(r => r.RequesterEmployeeId == employeeId && (status == null || r.Status == status))
    {
        AddInclude(r => r.Steps);
        AddOrderByDescending(r => r.CreatedAtUtc);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class ApprovalInboxSpecification : BaseSpecfication<ApprovalStep>
{
    public ApprovalInboxSpecification(Guid employeeId, string[] permissions, int? skip = null, int? take = null)
        : base(s => s.Status == ApprovalStepStatus.Pending
            && s.Request.Status == ApprovalStatus.Pending
            && s.Request.RequesterEmployeeId != employeeId
            && ((s.Kind == ApprovalStepKind.Manager && s.ApproverEmployeeId == employeeId)
                || (s.Kind == ApprovalStepKind.Permission && s.Permission != null && permissions.Contains(s.Permission))))
    {
        AddInclude(s => s.Request);
        AddOrderBy(s => s.Request.CreatedAtUtc);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class ApprovalRequestExistsForSubjectSpecification : BaseSpecfication<ApprovalRequest>
{
    public ApprovalRequestExistsForSubjectSpecification(ApprovalSubjectType type, Guid subjectId)
        : base(r => r.SubjectType == type && r.SubjectId == subjectId) {}
}