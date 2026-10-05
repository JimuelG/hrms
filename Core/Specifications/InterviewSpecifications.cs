using Core.Entities;

namespace Core.Specifications;
public sealed class InterviewsByApplicationSpecification : BaseSpecfication<Interview>
{
    public InterviewsByApplicationSpecification(Guid applicationId) : base(i => i.ApplicationId == applicationId)
    {
        AddInclude(i => i.Application);
        AddInclude(i => i.Interviewer);
        AddInclude(i => i.Evaluation!);
        AddOrderByDescending(i => i.ScheduledAtUtc);
    }
}

public sealed class InterviewByInterviewerSpecification : BaseSpecfication<Interview>
{
    public InterviewByInterviewerSpecification(Guid interviewerId, DateTime? fromUtc = null)
        : base(i => i.InterviewerId == interviewerId && (fromUtc == null || i.ScheduledAtUtc >= fromUtc))
    {
        AddInclude(i => i.Application);
        AddInclude(i => i.Interviewer);
        AddInclude(i => i.Evaluation!);
        AddOrderBy(i => i.ScheduledAtUtc);
    }
}

public sealed class InterviewByIdWithRelationsSpecification : BaseSpecfication<Interview>
{
    public InterviewByIdWithRelationsSpecification(Guid id) : base(i => i.Id == id)
    {
        AddInclude(i => i.Application);
        AddInclude(i => i.Interviewer);
        AddInclude(i => i.Evaluation!);
    }
}