using Core.Common;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Payroll;

public sealed class CompensationService(
    IUnitOfWork unit,
    IGenericRepository<EmployeeCompensation> compensation,
    IGenericRepository<Employee> employees,
    IUserDirectory users,
    ITenantContext tenant,
    ITimeZoneResolver zones,
    TimeProvider clock) : ICompensationService
{
    public async Task<ServiceResult<IReadOnlyList<CompensationDto>>> GetHistoryAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null)
            return ServiceResult<IReadOnlyList<CompensationDto>>.Fail("Employee not found.", ServiceErrorType.NotFound);

        return ServiceResult<IReadOnlyList<CompensationDto>>.Success(await BuildAsync(employee, ct));
    }

    public async Task<ServiceResult<CompensationDto>> SetAsync(Guid actorUserId, Guid employeeId, SetCompensationDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ServiceResult<CompensationDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        var reason = dto.Reason.Trim();
        if (reason.Length == 0) return ServiceResult<CompensationDto>.Fail("A reason is required.");
        
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await zones.ResolveAsync(employee, ct));
        var effective = dto.EffectiveFrom!.Value;

        var history = await compensation.ListAsync(new CompensationByEmployeeSpefication(employeeId), ct);
        DateOnly? latest = history.Count == 0 ? null : history.Max(h => h.EffectiveFrom);

        var error = CompensationRules.Check(dto.MonthlyBasicSalary, effective, employee.HireDate, latest, today);
        if (error is not null) return ServiceResult<CompensationDto>.Fail(error);

        var row = new EmployeeCompensation
        {
            EmployeeId = employeeId,
            EffectiveFrom = effective,
            MonthlyBasicSalary = dto.MonthlyBasicSalary,
            Reason = reason,
            CreatedByUserId = actorUserId
        };

        compensation.Add(row);
        await unit.Complete();

        var created = (await BuildAsync(employee, ct)).First(c => c.Id == row.Id);
        return ServiceResult<CompensationDto>.Success(created);
    }

    private async Task<IReadOnlyList<CompensationDto>> BuildAsync(Employee employee, CancellationToken ct)
    {
        var rows = await compensation.ListAsync(new CompensationByEmployeeSpefication(employee.Id), ct);
        var names = await users.GetDisplayNamesAsync(rows.Select(r => r.CreatedByUserId).Distinct().ToList(), tenant.RequiredTenantId, ct);

        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await zones.ResolveAsync(employee, ct));
        var currentFrom = rows.Where(r => r.EffectiveFrom <= today).Select(r => (DateOnly?)r.EffectiveFrom).Max();

        return rows.Select(r => new CompensationDto(
            r.Id,
            r.EmployeeId,
            r.EffectiveFrom,
            r.MonthlyBasicSalary,
            r.Reason,
            names.GetValueOrDefault(r.CreatedByUserId, "Unknown"),
            r.CreatedAtUtc,
            r.EffectiveFrom == currentFrom
        )).ToList();
    }
}