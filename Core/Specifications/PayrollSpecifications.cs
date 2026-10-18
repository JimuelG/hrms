using Core.Entities;

namespace Core.Specifications;
public sealed class CompensationByEmployeeSpefication : BaseSpecfication<EmployeeCompensation>
{
    public CompensationByEmployeeSpefication(Guid employeeId) : base(c => c.EmployeeId == employeeId)
    {
        AddOrderByDescending(c => c.EffectiveFrom);
    }
}