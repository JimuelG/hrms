using Core.Common;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Payroll;

public sealed class PayslipService(
    IGenericRepository<PayrollResultRecord> results,
    IGenericRepository<Employee> employees,
    ITenantNameProvider tenantNames) : IPayslipService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    private const string NotFound = "Payslip not found.";

    public async Task<ServiceResult<PayslipDto>> GetForEmployeeAsync(Guid runId, Guid employeeId, CancellationToken ct = default)
    {
        var row = await results.GetEntityWithSpec(new PayrollResultByRunAndEmployeeSpecification(runId, employeeId), ct);
        if (row is null || !PayslipRules.HrMayView(row.Run.Status, row.ErrorMessage))
            return ServiceResult<PayslipDto>.Fail(NotFound, ServiceErrorType.NotFound);

        return ServiceResult<PayslipDto>.Success(await ToDtoAsync(row, ct));
    }

    public async Task<ServiceResult<PayslipDto>> GetMineAsync(Guid userId, Guid runId, CancellationToken ct = default)
    {
        var me = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (me is null) return ServiceResult<PayslipDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var row = await results.GetEntityWithSpec(new PayrollResultByRunAndEmployeeSpecification(runId, me.Id), ct);
        if (row is null || !PayslipRules.EmployeeMayView(row.Run.Status, row.ErrorMessage))
            return ServiceResult<PayslipDto>.Fail(NotFound, ServiceErrorType.NotFound);

        return ServiceResult<PayslipDto>.Success(await ToDtoAsync(row, ct));
    }

    public async Task<ServiceResult<IReadOnlyList<PayslipSummaryDto>>> ListMineAsync(Guid userId, CancellationToken ct = default)
    {
        var me = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (me is null) return ServiceResult<IReadOnlyList<PayslipSummaryDto>>.Fail(NotLinked, ServiceErrorType.NotFound);

        var rows = await results.ListAsync(new PayrollResultsByEmployeeSpecification(me.Id), ct);
        return ServiceResult<IReadOnlyList<PayslipSummaryDto>>.Success(rows
            .Select(r => new PayslipSummaryDto(r.PayrollRunId, r.Run.PeriodStart, r.Run.PeriodEnd, r.Net, r.Run.FinalizedAtUtc, (int)r.Run.Kind))
            .ToList());
    }

    private async Task<PayslipDto> ToDtoAsync(PayrollResultRecord r, CancellationToken ct)
    {
        var earlier = await results.ListAsync(new FinalizedResultsForEmployeeInYearSpecification(r.EmployeeId, r.Run.PeriodEnd.Year, r.Run.PeriodStart), ct);
        var ytd = PayslipYtd.Compute(earlier, r);

        return new PayslipDto(
            r.PayrollRunId,
            (int)r.Run.Status,
            r.Run.PeriodStart,
            r.Run.PeriodEnd,
            r.Run.FinalizedAtUtc,
            await tenantNames.GetCurrentNameAsync(ct),
            r.EmployeeNumber,
            r.EmployeeName,
            r.PositionTitle,
            r.DepartmentName,
            r.MonthlyBasic,
            r.Gross,
            r.TaxableIncome,
            r.TotalEarnings,
            r.TotalDeductions,
            r.Net,
            ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>())
                .Select(l => new PayrollLineDto(l.Code, l.Label, l.Amount, (int)l.Kind)).ToList(),
            ResultSnapshot.Read(r.NotesJson, new List<string>()),
            new PayslipYtdDto(ytd.Gross, ytd.TaxableIncome, ytd.TaxWithheld, ytd.Net),
            (int)r.Run.Kind
        );
    }
}