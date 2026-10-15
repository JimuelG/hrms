using Core.Common;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class LeaveCalculationService(
    IGenericRepository<Employee> employees,
    IGenericRepository<LeaveType> leaveTypes,
    IGenericRepository<Holiday> holidays,
    IGenericRepository<WorkSchedule> schedules,
    IGenericRepository<TenantSettings> settings) : ILeaveCalculationService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    public async Task<ServiceResult<LeaveCalculation>> CalculateAsync(Employee employee, LeaveType type, DateOnly start, DateOnly end, DayPortion startPortion, DayPortion endPortion, CancellationToken ct = default)
    {
        var workingDays = await WorkingDaysAsync(employee, ct);

        IReadOnlyDictionary<DateOnly, string> holidayMap = new Dictionary<DateOnly, string>();
        var span = end.DayNumber - start.DayNumber + 1;
        if (span >= 1 && span <= LeaveCalculator.MaxRangeDays)
        {
            holidayMap = (await holidays.ListAsync(new HolidaysInRangeSpecification(start, end), ct))
                .ToDictionary(h => h.Date, h => h.Name);
        }

        var result = LeaveCalculator.Calculate(start, end, startPortion, endPortion, workingDays, holidayMap, type.AllowsHalfDay);
        return result.Error is null
            ? ServiceResult<LeaveCalculation>.Success(result)
            : ServiceResult<LeaveCalculation>.Fail(result.Error);
    }

    public async Task<ServiceResult<LeavePreviewDto>> PreviewAsync(Guid userId, LeavePreviewQuery query, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<LeavePreviewDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var type = await leaveTypes.GetByIdAsync(query.LeaveTypeId!.Value, ct);
        if (type is null) return ServiceResult<LeavePreviewDto>.Fail("Leave type not found.", ServiceErrorType.NotFound);
        if (!type.IsActive) return ServiceResult<LeavePreviewDto>.Fail("That leave type is no longer available.");

        if (!Enum.IsDefined(typeof(DayPortion), query.StartPortion) || !Enum.IsDefined(typeof(DayPortion), query.EndPortion)) 
            return ServiceResult<LeavePreviewDto>.Fail("Invalid day portion.");

        var calc = await CalculateAsync(
            employee,
            type,
            query.StartDate!.Value,
            query.EndDate!.Value,
            (DayPortion)query.StartPortion,
            (DayPortion)query.EndPortion,
            ct);

        var c = calc.Value!;
        return ServiceResult<LeavePreviewDto>.Success(new LeavePreviewDto(
            c.Days,
            LeaveCalculator.RequiresDocument(c.Days, type.RequiresDocumentAfterDays),
            type.IsPaid,
            c.Lines.Select(l => new LeaveDayLineDto(l.Date, l.Value, (int)l.Status, l.HolidayName)).ToList()));
    }

    private async Task<WorkingDay> WorkingDaysAsync(Employee employee, CancellationToken ct)
    {
        if (employee.ScheduleId is Guid sid)
        {
            var schedule = await schedules.GetByIdAsync(sid, ct);
            if (schedule is not null) return schedule.WorkingDays;
        }

        var s = await settings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        return s?.WorkingDays ?? new TenantSettings().WorkingDays;
    }
}