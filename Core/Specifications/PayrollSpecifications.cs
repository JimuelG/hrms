using Core.Common;
using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class CompensationByEmployeeSpefication : BaseSpecfication<EmployeeCompensation>
{
    public CompensationByEmployeeSpefication(Guid employeeId) : base(c => c.EmployeeId == employeeId)
    {
        AddOrderByDescending(c => c.EffectiveFrom);
    }
}

public sealed class StatutoryRuleSetsSpecification : BaseSpecfication<StatutoryRuleSet>
{
    public StatutoryRuleSetsSpecification(StatutoryKind? kind) : base(r => kind == null || r.Kind == kind)
    {
        AddInclude(r => r.Brackets);
        AddOrderByDescending(r => r.EffectiveFrom);
    }
}

public sealed class StatutoryRuleSetByIdSpecificaiton :  BaseSpecfication<StatutoryRuleSet>
{
    public StatutoryRuleSetByIdSpecificaiton(Guid id) : base(r => r.Id == id)
    {
        AddInclude(r => r.Brackets);
    }
}

public sealed class StatutoryRuleSetDuplicationSpecification : BaseSpecfication<StatutoryRuleSet>
{
    public StatutoryRuleSetDuplicationSpecification(StatutoryKind kind, PayFrequency? basis, DateOnly effectiveFrom)
        : base(r => r.Kind == kind && r.Basis == basis && r.EffectiveFrom == effectiveFrom) { }
}

public sealed class PayrollRunByIdSpecification : BaseSpecfication<PayrollRun>
{
    public PayrollRunByIdSpecification(Guid id, bool withAdjustments = false) : base(r => r.Id == id)
    {
        if (withAdjustments) AddInclude(r => r.Adjustments);
    }
}

public sealed class PayrollRunsSpecification : BaseSpecfication<PayrollRun>
{
    public PayrollRunsSpecification() : base(r => true)
    {
        AddOrderByDescending(r => r.PeriodStart);
    }
}

public sealed class PayrollRunByPeriodSpecification : BaseSpecfication<PayrollRun>
{
    public PayrollRunByPeriodSpecification(DateOnly start, DateOnly end) : base(r => r.PeriodStart == start && r.PeriodEnd == end && r.Status != PayrollRunStatus.Cancelled) { }
}

public sealed class PayrollResultsByRunSpecification : BaseSpecfication<PayrollResultRecord>
{
    public PayrollResultsByRunSpecification(Guid runId) : base(r => r.PayrollRunId == runId)
    {
        AddOrderBy(r => r.EmployeeName);
    }
}

public sealed class PayrollSettingsSpecification : BaseSpecfication<PayrollSettingsRecord>
{
    public PayrollSettingsSpecification() : base(s => true) { }
}

public sealed class EmployeesForPayrollSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] NotYetEmloyed = [EmployeeStatus.Applicant, EmployeeStatus.PreOnboarding];

    public EmployeesForPayrollSpecification(DateOnly periodStart, DateOnly periodEnd) 
        : base(e => e.HireDate <= periodEnd 
            && !NotYetEmloyed.Contains(e.Status)
            && (e.LastWorkingDay == null || e.LastWorkingDay >= periodStart))
    {
        AddInclude(e => e.Department);
        AddInclude(e => e.Position);
        AddOrderBy(e => e.LastName);
    }
}

public sealed class CompensationForEmployeesSpecification : BaseSpecfication<EmployeeCompensation>
{
    public CompensationForEmployeesSpecification(List<Guid> employeeIds) : base(c => employeeIds.Contains(c.EmployeeId)) { }
}

public sealed class ApprovedUnpaidLeaveSpecification : BaseSpecfication<LeaveRequest>
{
    public ApprovedUnpaidLeaveSpecification(DateOnly from, DateOnly to) 
        : base(r => r.Status == ApprovalStatus.Approved && !r.IsPaid && r.StartDate <= to && r.EndDate >= from) {}
}

public sealed class ApprovedLeaveSpecification : BaseSpecfication<LeaveRequest>
{
    public ApprovedLeaveSpecification(DateOnly from, DateOnly to)
        : base(r => r.Status == ApprovalStatus.Approved && r.StartDate <= to && r.EndDate >= from) { }
}

public sealed class ApprovedOvertimeInRangeSpecification : BaseSpecfication<OvertimeRequest>
{
    public ApprovedOvertimeInRangeSpecification(DateOnly from, DateOnly to)
        : base(r => r.Status == ApprovalStatus.Approved && r.Date >= from && r.Date <= to) { }
}

public sealed class PayrollAdjustmentByIdSpecification : BaseSpecfication<PayrollAdjustmentRecord>
{
    public PayrollAdjustmentByIdSpecification(Guid id, Guid runId) : base(a => a.Id == id && a.PayrollRunId == runId) { }
}

public sealed class PayrollResultsByEmployeeSpecification : BaseSpecfication<PayrollResultRecord>
{
    public PayrollResultsByEmployeeSpecification(Guid employeeId)
        : base(r => r.EmployeeId == employeeId && r.ErrorMessage == null && r.Run.Status == PayrollRunStatus.Finalized)
    {
        AddInclude(r => r.Run);
        AddOrderByDescending(r => r.Run.PeriodStart);
    }
}

public sealed class PayrollResultByRunAndEmployeeSpecification : BaseSpecfication<PayrollResultRecord>
{
    public PayrollResultByRunAndEmployeeSpecification(Guid runId, Guid employeeId)
        : base(r => r.PayrollRunId == runId && r.EmployeeId == employeeId)
    {
        AddInclude(r => r.Run);
    }
}

public sealed class RecurringItemsByEmployeeSpecification : BaseSpecfication<RecurringPayrollItem>
{
    public RecurringItemsByEmployeeSpecification(Guid employeeId) : base(i => i.EmployeeId == employeeId)
    {
        AddOrderByDescending(i => i.From);
    }
}

public sealed class ActiveRecurringItemsInPeriodSpecification : BaseSpecfication<RecurringPayrollItem>
{
    public ActiveRecurringItemsInPeriodSpecification(DateOnly from, DateOnly to)
        : base(i => i.IsActive && i.From <= to && (i.To == null || i.To >= from))
    {
        
    }
}

public sealed class RecurringItemsByIdsSpecification : BaseSpecfication<RecurringPayrollItem>
{
    public RecurringItemsByIdsSpecification(List<Guid> ids) : base(i => ids.Contains(i.Id)) {}
}

public sealed class LoanApplicationsByRunSpecification : BaseSpecfication<PayrollLoanApplication>
{
    public LoanApplicationsByRunSpecification(Guid runId) : base(a => a.PayrollRunId == runId) { }
}
