using Core.Entities;

namespace Core.Specifications;
public sealed class ActiveTaskTemplatesSpecification : BaseSpecfication<OnboardingTaskTemplate>
{
    public ActiveTaskTemplatesSpecification() : base(t => t.IsActive)
    {
        AddOrderBy(t => t.SortOrder);
    }
}

public sealed class OnboardingCaseByIdWithTasksSpecification : BaseSpecfication<OnboardingCase>
{
    public OnboardingCaseByIdWithTasksSpecification(Guid id) : base(c => c.Id == id)
    {
        AddInclude(c => c.Application);
        AddInclude(c => c.Tasks);
    }
}

public sealed class OnboardingCaseByApplicationSpecification : BaseSpecfication<OnboardingCase>
{
    public OnboardingCaseByApplicationSpecification(Guid applicationId) : base(c => c.ApplicationId == applicationId)
    {
        AddInclude(c => c.Application);
        AddInclude(c => c.Tasks);
    }
}