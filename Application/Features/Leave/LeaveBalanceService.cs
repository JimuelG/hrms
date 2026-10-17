using System.IO.Pipelines;
using Core.Common;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class LeaveBalanceService(
    IUnitOfWork unit,
    IGenericRepository<LeaveBalanceEntry> ledger,
    IGenericRepository<LeaveType> leaveTypes,
    IGenericRepository<Employee> employees,
    ITimeZoneResolver zones,
    TimeProvider clock) : ILeaveBalanceService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    private const int PostYearsBack = 2;
    private const int PostYearsAhead = 1;

    public Task<ServiceResult<LeaveBalanceDto>> AdjustAsync(Guid actorUserId, PostLedgerEntryDto dto, CancellationToken ct = default) =>
        PostAsync(actorUserId, dto, LeaveEntryType.Adjustment, ct);

    public async Task EnsureGrantsAsync(Employee employee, CancellationToken ct = default)
    {
        if (!LeaveEligibility.IsEmployed(employee.Status)) return;

        var today = await TodayAsync(employee, ct);
        var types = await leaveTypes.ListAsync(new AnnualLeaveTypesSpecification(), ct);
        if (types.Count == 0) return;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var rows = await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employee.Id), ct);
            var staged = 0;

            foreach (var type in types)
            {
                var existing = rows.Where(r => r.LeaveTypeId == type.Id).Select(ToRow).ToList();
                var policy = new LeavePolicy(type.DefaultDays, type.ProrateFirstYear, type.MaxCarryOverDays, type.MinServiceMonths);

                foreach (var p in LeaveGrantPlanner.Plan(policy, employee.HireDate, today, existing))
                {
                    ledger.Add(new LeaveBalanceEntry
                    {
                        EmployeeId = employee.Id,
                        LeaveTypeId = type.Id,
                        Year = p.Year,
                        EntryType = p.Type,
                        Days = p.Days,
                        Description = p.Description,
                        IsAutomatic = true
                    });
                    staged++;
                }
            }

            if (staged == 0) return;

            try
            {
                await unit.Complete();
                return;
            }
            catch (DuplicateKeyException) when (attempt == 0)
            {
                
            }
        }
    }

    public async Task<ServiceResult<LeaveBalancesDto>> GetForEmployeeAsync(Guid employeeId, int? year, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        return employee is null
            ? ServiceResult<LeaveBalancesDto>.Fail("Employee not found", ServiceErrorType.NotFound)
            : await BuildBalancesAsync(employee, year, ct);
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>> GetLedgerForEmployeeAsync(Guid employeeId, int? year, Guid? leaveTypeId, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        return employee is null
            ? ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>.Fail("Employee not found.", ServiceErrorType.NotFound)
            : await BuildLedgerAsync(employee, year, leaveTypeId, ct);
    }

    public async Task<ServiceResult<LeaveBalancesDto>> GetMineAsync(Guid userId, int? year, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        return employee is null
            ? ServiceResult<LeaveBalancesDto>.Fail(NotLinked, ServiceErrorType.NotFound)
            : await BuildBalancesAsync(employee, year, ct);
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>> GetMyLedgerAsync(Guid userId, int? year, Guid? leaveTypeId, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        return employee is null
            ? ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>.Fail(NotLinked, ServiceErrorType.NotFound)
            : await BuildLedgerAsync(employee, year, leaveTypeId, ct);
    }

    public Task<ServiceResult<LeaveBalanceDto>> GrantAsync(Guid actorUserId, PostLedgerEntryDto dto, CancellationToken ct = default) =>
        PostAsync(actorUserId, dto, LeaveEntryType.Grant, ct);

    private async Task<ServiceResult<LeaveBalanceDto>> PostAsync(
        Guid actorUserId, PostLedgerEntryDto dto, LeaveEntryType kind, CancellationToken ct)
    {
        var employee = await employees.GetByIdAsync(dto.EmployeeId!.Value, ct);
        if (employee is null) return ServiceResult<LeaveBalanceDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        var type = await leaveTypes.GetByIdAsync(dto.LeaveTypeId!.Value, ct);
        if (type is null) return ServiceResult<LeaveBalanceDto>.Fail("Leave type not found.", ServiceErrorType.NotFound);

        if (type.EntitlementMode == LeaveEntitlementMode.Unlimited)
            return ServiceResult<LeaveBalanceDto>.Fail("This leave type has no balance to change.");

        var reason = dto.Reason.Trim();
        if (reason.Length == 0) return ServiceResult<LeaveBalanceDto>.Fail("A reason is required.");
        
        var today = await TodayAsync(employee, ct);
        if (dto.Year < today.Year - PostYearsBack || dto.Year > today.Year + PostYearsAhead)
            return ServiceResult<LeaveBalanceDto>.Fail($"Year must be between {today.Year - PostYearsBack} and {today.Year - PostYearsAhead}");

        if (dto.Days == 0 || dto.Days % 0.5m != 0 || Math.Abs(dto.Days) > 366)
            return ServiceResult<LeaveBalanceDto>.Fail("Days must be a non-zero amount in steps of half a day, up to 366.");

        if (kind == LeaveEntryType.Grant)
        {
            if (type.EntitlementMode != LeaveEntitlementMode.HrGranted)
                return ServiceResult<LeaveBalanceDto>.Fail("Only HR-granted leave type are granted manuall, Use an adjustment for other types.");
            if (dto.Days < 0)
                return ServiceResult<LeaveBalanceDto>.Fail("A grant must be a positive number of days. Use an adjustment to remove days");
        }

        await EnsureGrantsAsync(employee, ct);

        var yearRows = (await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employee.Id), ct))
            .Where(r => r.LeaveTypeId == type.Id && r.Year == dto.Year)
            .ToList();

        var current = LeaveBalanceMath.Summarize(yearRows.Select(ToRow)).Balance;
        if (current + dto.Days < 0)
            return ServiceResult<LeaveBalanceDto>.Fail($"That would take the balance below zero (current balance: {current:0.#}).");
        
        var entry = new LeaveBalanceEntry
        {
            EmployeeId = employee.Id, 
            LeaveTypeId = type.Id,
            Year = dto.Year,
            EntryType = kind,
            Days = dto.Days,
            Description = reason,
            IsAutomatic = false,
            CreatedByUserId = actorUserId
        };
        
        ledger.Add(entry);
        await unit.Complete();

        yearRows.Add(entry);
        return ServiceResult<LeaveBalanceDto>.Success(ToBalance(employee, type, dto.Year, yearRows, today));
    }

    private async Task<ServiceResult<LeaveBalancesDto>> BuildBalancesAsync(Employee employee, int? year, CancellationToken ct)
    {
        var yearError = ValidateYear(year);
        if (yearError is not null) return ServiceResult<LeaveBalancesDto>.Fail(yearError);

        await EnsureGrantsAsync(employee, ct);

        var today = await TodayAsync(employee, ct);
        var y = year ?? today.Year;

        var rows = await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employee.Id), ct);
        var types = await leaveTypes.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct);

        var items = new List<LeaveBalanceDto>();
        foreach (var type in types.Where(t => t.EntitlementMode != LeaveEntitlementMode.Unlimited))
        {
            var yearRows = rows.Where(r => r.LeaveTypeId == type.Id && r.Year == y).ToList();

            var show = yearRows.Count > 0 || (type.EntitlementMode == LeaveEntitlementMode.Annual && type.IsActive);
            if (show) items.Add(ToBalance(employee, type, y, yearRows, today));
        }

        return ServiceResult<LeaveBalancesDto>.Success(new LeaveBalancesDto(y, today, items));
    }

    private async Task<ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>> BuildLedgerAsync(Employee employee, int? year, Guid? leaveTypeId, CancellationToken ct)
    {
        var yearError = ValidateYear(year);
        if (yearError is not null) return ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>.Fail(yearError);

        await EnsureGrantsAsync(employee, ct);

        var rows = await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employee.Id), ct);
        var types = (await leaveTypes.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct)).ToDictionary(t => t.Id);

        var entries = rows
            .Where(r => (year is null || r.Year == year) && (leaveTypeId is null || r.LeaveTypeId == leaveTypeId))
            .Select(r =>
            {
                types.TryGetValue(r.LeaveTypeId, out var t);
                return new LeaveLedgerEntryDto(
                    r.Id,
                    r.LeaveTypeId,
                    t?.Code ?? "?",
                    t?.Name ?? "Unknown",
                    r.Year,
                    (int)r.EntryType,
                    r.Days,
                    r.Description,
                    r.IsAutomatic,
                    r.CreatedAtUtc);
            }).ToList();

        return ServiceResult<IReadOnlyList<LeaveLedgerEntryDto>>.Success(entries);
    }

    private async Task<DateOnly> TodayAsync(Employee employee, CancellationToken ct) =>
        LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await zones.ResolveAsync(employee, ct));

    private static string? ValidateYear(int? year) =>
        year is int y && (y < LeaveCalculator.MinYear || y > LeaveCalculator.MaxYear)
            ? $"Year must be between {LeaveCalculator.MinYear} and {LeaveCalculator.MaxRangeDays}."
            : null;

    private static LedgerRow ToRow(LeaveBalanceEntry r) => new (r.Year, r.EntryType, r.Days, r.IsAutomatic);

    private static LeaveBalanceDto ToBalance(Employee employee, LeaveType type, int year, IEnumerable<LeaveBalanceEntry> yearRows, DateOnly today)
    {
        var s = LeaveBalanceMath.Summarize(yearRows.Select(ToRow));

        DateOnly? eligibleFrom = type.EntitlementMode == LeaveEntitlementMode.Annual
            ? LeaveGrantPlanner.EligibleFrom(employee.HireDate, type.MinServiceMonths)
            : null;

        return new LeaveBalanceDto(
            type.Id,
            type.Code,
            type.Name,
            type.IsPaid,
            (int)type.EntitlementMode,
            year,
            s.Granted,
            s.CarriedOver,
            s.Adjusted,
            s.Used,
            s.Balance,
            eligibleFrom,
            eligibleFrom is null || eligibleFrom <= today
        );
    }
}