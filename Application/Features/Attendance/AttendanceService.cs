using Core.Common;
using Core.DTOs.Attendance;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Attendance;

public class AttendanceService(
    IUnitOfWork unit,
    IGenericRepository<AttendanceRecord> records,
    IGenericRepository<Employee> employees,
    IGenericRepository<EmployeeWorkLocation> assignments,
    IGenericRepository<WorkLocation> locations,
    IGenericRepository<WorkSchedule> schedules,
    IGenericRepository<Branch> branches,
    IGenericRepository<TenantSettings> settings,
    TimeProvider clock) : IAttendanceService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    private static readonly EmployeeStatus[] CanClockIn = [EmployeeStatus.Probationary, EmployeeStatus.Regular];
    private static readonly TimeSpan LongShift = TimeSpan.FromHours(16);

    public async Task<ServiceResult<AttendanceRecordDto>> ClockInAsync(Guid userId, ClockRequestDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<AttendanceRecordDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        if (!CanClockIn.Contains(employee.Status))
            return ServiceResult<AttendanceRecordDto>.Fail("Your employment status doesn't allow clocking in. Contact HR.");

        var now = clock.GetUtcNow().UtcDateTime;
        var today = LocalClock.LocalDate(now, await TimeZoneIdAsync(employee, ct));

        if (await records.GetEntityWithSpec(new OpenAttendanceByEmployeeSpecificaton(employee.Id), ct) is not null)
            return ServiceResult<AttendanceRecordDto>.Fail("You're already clocked in. Clock out first, or ask HR to correct the ealier entry.", ServiceErrorType.Conflict);

        if (await records.CountAsync(new AttendanceByEmployeeAndDateSpecification(employee.Id, today), ct) > 0)
            return ServiceResult<AttendanceRecordDto>.Fail("You've already clocked in and out today, Ask HR to correct your attendance if this was a mistake.", ServiceErrorType.Conflict);

        var r = await ResolveAsync(employee, dto, today, ClockEvent.In, ct);
        if (r.Error is not null) return ServiceResult<AttendanceRecordDto>.Fail(r.Error);

        var schedule = employee.ScheduleId is Guid sid ? await schedules.GetByIdAsync(sid, ct) : null;

        var attendance = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Date = today,
            ClockInUtc = now,
            ClockInWorkLocationId = r.LocationId,
            ClockInLocationName = r.LocationName,
            ClockInDistanceMeters = r.Distance,
            ClockInAccuracyMeters = r.Accuracy,
            ClockInLatitude = r.Latitude,
            ClockInLongitude = r.Longitude,
            ScheduledStart = schedule?.StartTime,
            ScheduleEnd = schedule?.EndTime,
            Flags = r.Flagged ? AttendanceFlags.ClockInLocationUnverified : AttendanceFlags.None
        };

        records.Add(attendance);
        await unit.Complete();

        return ServiceResult<AttendanceRecordDto>.Success(await LoadDtoAsync(attendance.Id, ct));
    }

    public async Task<ServiceResult<AttendanceRecordDto>> ClockOutAsync(Guid userId, ClockRequestDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<AttendanceRecordDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var attendance = await records.GetEntityWithSpec(new OpenAttendanceByEmployeeSpecificaton(employee.Id), ct);
        if (attendance is null)
            return ServiceResult<AttendanceRecordDto>.Fail("You're not clocked in.", ServiceErrorType.Conflict);

        var now = clock.GetUtcNow().UtcDateTime;
        var today = LocalClock.LocalDate(now, await TimeZoneIdAsync(employee, ct));

        var r =await ResolveAsync(employee, dto, today, ClockEvent.Out, ct);
        if (r.Error is not null) return ServiceResult<AttendanceRecordDto>.Fail(r.Error);

        attendance.ClockOutUtc = now;
        attendance.ClockOutWorkLocationId = r.LocationId;
        attendance.ClockOutLocationName = r.LocationName;
        attendance.ClockOutDistanceMeters = r.Distance;
        attendance.ClockOutAccuracyMeters = r.Accuracy;
        attendance.ClockOutLatitude = r.Latitude;
        attendance.ClockOutLongitude = r.Longitude;

        if (r.Flagged) attendance.Flags |= AttendanceFlags.ClockOutLocationUnverified;
        if (now - attendance.ClockInUtc > LongShift) attendance.Flags |= AttendanceFlags.LongShift;

        await unit.Complete();
        return ServiceResult<AttendanceRecordDto>.Success(await LoadDtoAsync(attendance.Id, ct));
    }

    public async Task<ServiceResult<IReadOnlyList<AttendanceRecordDto>>> GetMineAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null)
            return ServiceResult<IReadOnlyList<AttendanceRecordDto>>.Fail(NotLinked, ServiceErrorType.NotFound);

        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await TimeZoneIdAsync(employee, ct));
        var end = to ?? today;
        var start = from ?? end.AddDays(-13);

        if (end < start) return ServiceResult<IReadOnlyList<AttendanceRecordDto>>.Fail("'from' must be on or before 'to'.");
        if (end.DayNumber - start.DayNumber > 62)
            return ServiceResult<IReadOnlyList<AttendanceRecordDto>>.Fail("The date range can't exceed 62 days.");

        var rows = await records.ListAsync(new AttendanceByEmployeeRangeSpecification(employee.Id, start, end), ct);
        return ServiceResult<IReadOnlyList<AttendanceRecordDto>>.Success(rows.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<PagedResult<AttendanceRecordDto>>> QueryAsync(DateOnly from, DateOnly to, Guid? employeeId, bool needsReview, int page, int pageSize, CancellationToken ct = default)
    {
        if (to < from) return ServiceResult<PagedResult<AttendanceRecordDto>>.Fail("'from' must be on or before 'to'.");
        if (to.DayNumber - from.DayNumber > 31)
            return ServiceResult<PagedResult<AttendanceRecordDto>>.Fail("The date range can't exceed 31 days.");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await records.CountAsync(new AttendanceQuerySpecification(from, to, employeeId, needsReview), ct);
        var rows = await records.ListAsync(
            new AttendanceQuerySpecification(from, to, employeeId, needsReview, (page - 1) * pageSize, pageSize), ct);

        return ServiceResult<PagedResult<AttendanceRecordDto>>.Success(
            new PagedResult<AttendanceRecordDto>(rows.Select(ToDto).ToList(), total, page, pageSize));
    }

    private sealed record Resolution(
        string? Error = null,
        Guid? LocationId = null,
        string? LocationName = null,
        double? Distance = null,
        bool Flagged = false,
        double? Latitude = null,
        double? Longitude = null,
        double? Accuracy = null);

    private async Task<Resolution> ResolveAsync(Employee employee, ClockRequestDto dto, DateOnly today, ClockEvent evt, CancellationToken ct)
    {
        var hasAll = dto.Latitude is not null && dto.Longitude is not null && dto.AccuracyMeters is not null;
        var hasAny = dto.Latitude is not null || dto.Longitude is not null || dto.AccuracyMeters is not null;
        if (hasAny && !hasAll)
            return new Resolution(Error: "Latitude, longitude and accuracy must be sent together.");

        var usable = await UsableLocationAsync(employee.Id, today, ct);

        WorkLocation? chosen = null;
        if (dto.WorkLocationId is Guid wid)
        {
            chosen = usable.FirstOrDefault(l => l.Id == wid);
            if (chosen is null)
                return new Resolution(Error: "That work location isn't assigned to you or isn't available today.");

            if (chosen.Type == WorkLocationType.Remote)
                return new Resolution(LocationId: chosen.Id, LocationName: chosen.Name);
        }

        var pool = chosen is null ? usable : new List<WorkLocation> { chosen };
        var candidates = pool
            .Where(l => l.Type != WorkLocationType.Remote
                    && l.Latitude.HasValue && l.Longitude.HasValue && l.RadiusMeters.HasValue)
            .Select(l => new GeofenceCandidate(l.Id, l.Name, l.Latitude!.Value, l.Longitude!.Value, l.RadiusMeters!.Value))
            .ToList();

        if (evt == ClockEvent.In)
        {
            if (usable.Count == 0)
                return new Resolution(Error: "No work location is assigned to you for today. Contact HR.");
            if (candidates.Count == 0)
                return new Resolution(Error: "Choose your remote work location to clock in.");
        }

        GeofenceResult? result = hasAll && candidates.Count > 0
            ? GeofenceEvaluator.Evaluate(dto.Latitude!.Value, dto.Latitude!.Value, dto.AccuracyMeters!.Value, candidates)
            : null;

        var decision = ClockPolicy.Decide(evt, result, dto.AccuracyMeters);
        if (!decision.Accepted) return new Resolution(Error: decision.RejectReason);

        var matched = result is { Outcome: GeofenceOutcome.Inside or GeofenceOutcome.Borderline};
        var keepCoordinates = decision.Flagged && hasAll;

        return new Resolution(
            LocationId: matched ? result!.LocationId : null,
            LocationName: matched ? result!.LocationName : null,
            Distance: matched && result!.DistanceMeters is double d ? Math.Round(d, 1) : null,
            Flagged: decision.Flagged,
            Latitude: keepCoordinates ? dto.Latitude : null,
            Longitude: keepCoordinates ? dto.Longitude : null,
            Accuracy: hasAll ? dto.AccuracyMeters : null);
    }

    private async Task<List<WorkLocation>> UsableLocationAsync(Guid employeeId, DateOnly today, CancellationToken ct)
    {
        var rows = await assignments.ListAsync(new AssignmentsByIdEmployeeSpecification(employeeId), ct);
        if (rows.Count == 0) return [];

        var ids = rows.Select(r => r.WorkLocationId).ToList();
        return (await locations.ListAsync(new WorkLocationsByIdsSpecification(ids), ct))
            .Where(l => l.IsActive
                && (l.Type != WorkLocationType.Temporary || (l.ValidFrom <= today && today <= l.ValidTo)))
            .ToList();
    }

    private async Task<string?> TimeZoneIdAsync(Employee employee, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(employee.BranchId, ct);
        if (!string.IsNullOrWhiteSpace(branch?.TimeZoneId)) return branch!.TimeZoneId;

        var s = await settings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        return s?.TimeZoneId;
    }

    private async Task<AttendanceRecordDto> LoadDtoAsync(Guid id, CancellationToken ct) =>
        ToDto((await records.GetEntityWithSpec(new AttendanceByIdWithEmployeeSpecification(id), ct))!);
    
    private static AttendanceRecordDto ToDto(AttendanceRecord a) => new(
        a.Id,
        a.EmployeeId,
        a.Employee.EmployeeNumber,
        $"{a.Employee.FirstName} {a.Employee.LastName}",
        a.Date,
        a.ClockInUtc,
        a.ClockInLocationName,
        a.ClockInDistanceMeters,
        a.ClockInAccuracyMeters,
        a.ClockInLatitude,
        a.ClockInLongitude,
        a.ClockOutUtc,
        a.ClockOutLocationName,
        a.ClockOutDistanceMeters,
        a.ClockOutAccuracyMeters,
        a.ClockOutLatitude,
        a.ClockOutLongitude,
        a.ClockOutUtc is null ? null : (int)Math.Floor((a.ClockOutUtc.Value - a.ClockInUtc).TotalMinutes),
        (int)a.Flags);
}