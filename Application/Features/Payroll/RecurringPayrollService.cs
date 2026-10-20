using Core.Common;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Payroll;

public class RecurringPayrollService(
    IUnitOfWork unit,
    IGenericRepository<RecurringPayrollItem> items,
    IGenericRepository<Employee> employees) : IRecurringPayrollService
{
    public async Task<ServiceResult<RecurringItemDto>> CreateAsync(Guid actorUserId, CreateRecurringItemDto dto, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(dto.EmployeeId!.Value, ct) is null)
            return ServiceResult<RecurringItemDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        if (!Enum.IsDefined(typeof(AdjustmentTax), dto.Tax))
            return ServiceResult<RecurringItemDto>.Fail("Choose a valid tax treatment.");

        var from = dto.From!.Value;
        if (from.Year < LeaveCalculator.MinYear || from.Year > LeaveCalculator.MaxYear)
            return ServiceResult<RecurringItemDto>.Fail("The start date is out of range.");

        var error = RecurringRules.CheckRule(dto.Label, dto.Amount, from, dto.To, dto.TotalOwed);
        if (error is not null) return ServiceResult<RecurringItemDto>.Fail(error);

        var item = new RecurringPayrollItem
        {
            EmployeeId = dto.EmployeeId.Value,
            Label = dto.Label.Trim(),
            Amount = dto.Amount,
            Tax = (AdjustmentTax)dto.Tax,
            From = from,
            To = dto.To,
            TotalOwed = dto.TotalOwed,
            CreatedByUserId = actorUserId
        };

        items.Add(item);
        await unit.Complete();

        return ServiceResult<RecurringItemDto>.Success(ToDto(item));
    }

    public async Task<ServiceResult<RecurringItemDto>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await items.GetByIdAsync(id, ct);
        if (item is null) return ServiceResult<RecurringItemDto>.Fail("Recurring item not found.", ServiceErrorType.NotFound);

        item.IsActive = false;
        await unit.Complete();
        return ServiceResult<RecurringItemDto>.Success(ToDto(item));
    }

    public async Task<ServiceResult<IReadOnlyList<RecurringItemDto>>> ListAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<IReadOnlyList<RecurringItemDto>>.Fail("Employee not found.", ServiceErrorType.NotFound);

        var rows = await items.ListAsync(new RecurringItemsByEmployeeSpecification(employeeId), ct);
        return ServiceResult<IReadOnlyList<RecurringItemDto>>.Success(rows.Select(ToDto).ToList());
    }

    private static RecurringItemDto ToDto(RecurringPayrollItem i) => new(
        i.Id,
        i.EmployeeId,
        i.Label,
        i.Amount,
        (int)i.Tax,
        i.From,
        i.To,
        i.IsActive,
        i.TotalOwed,
        i.PaidSoFar,
        i.TotalOwed is decimal owed ? new LoanState(owed, i.PaidSoFar).Balance : null);
}