using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public class TimelineBySubjectSpecification : BaseSpecfication<TimelineEvent>
{
    public TimelineBySubjectSpecification(TimelineSubjectType subjectType, Guid subjectId)
        : base(e => e.SubjectType == subjectType && e.SubjectId == subjectId)
    {
        AddOrderByDescending(e => e.EventDateUtc);
    }
}