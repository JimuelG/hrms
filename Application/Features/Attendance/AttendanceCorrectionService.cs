using Core.Common;
using Core.DTOs.Attendance;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Attendance;

public class AttendanceCorrectionService(
    IUnitOfWork unit,
    IGenericRepository<AttendanceCorrectionRequest> requests,
    IGenericRepository<AttendanceRecord> records,
    IGenericRepository<Employee> employees,
    IGenericRepository<WorkSchedule> schedules,
    ITimeZoneResolver zones,
    ITimelineService timeline,
    TimeProvider clock) : IAttendanceCorrectionService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    private const int EmployeeWindowDays = 31;
    private const int ReviewerWindowDays = 366;
    private static readonly TimeSpan LongShift = TimeSpan.FromHours(16);

    private sealed record Problem(string Message, ServiceErrorType Type = ServiceErrorType.Validation);

    public async Task<ServiceResult<AttendanceCorrectionDto>> ApproveAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<AttendanceCorrectionDto>.Fail("Correction request not found.", ServiceErrorType.NotFound);
        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<AttendanceCorrectionDto>.Fail("Only a pending request can be approved.", ServiceErrorType.Conflict);

        if (await IsOwnAsync(reviewerUserId, request.EmployeeId, ct))
            return ServiceResult<AttendanceCorrectionDto>.Fail("You can't review your request. Ask another approver.");

        var employee = await employees.GetByIdAsync(request.EmployeeId, ct);
        if (employee is null) return ServiceResult<AttendanceCorrectionDto>.Fail("The employee no longer exists.", ServiceErrorType.NotFound);

        var now = clock.GetUtcNow().UtcDateTime;
        var applyError = await ApplyAsync(request, employee, now, ct);
        if (applyError is not null)
            return ServiceResult<AttendanceCorrectionDto>.Fail($"This correction can no longer be applied: {applyError}");

        request.Status = ApprovalStatus.Approved;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = now;
        request.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        RecordTimeline(request);
        await unit.Complete();
        return ServiceResult<AttendanceCorrectionDto>.Success(await LoadAsync(id, ct));
    }

    public async Task<ServiceResult<AttendanceCorrectionDto>> CancelAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<AttendanceCorrectionDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var request = await requests.GetByIdAsync(id, ct);
        if (request is null || request.EmployeeId != employee.Id)
            return ServiceResult<AttendanceCorrectionDto>.Fail("Correction request not found.", ServiceErrorType.NotFound);

        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<AttendanceCorrectionDto>.Fail("Only a pending request can be cancelled.", ServiceErrorType.Conflict);

        request.Status = ApprovalStatus.Cancelled;
        await unit.Complete();
        return ServiceResult<AttendanceCorrectionDto>.Success(await LoadAsync(id, ct));
    }

    public async Task<ServiceResult<AttendanceCorrectionDto>> CreateForEmployeeAsync(Guid reviewerUserId, Guid employeeId, CreateCorrectionDto dto, CancellationToken ct = default)
    {
        var target = await employees.GetByIdAsync(employeeId, ct);
        if (target is null) return ServiceResult<AttendanceCorrectionDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        if (await IsOwnAsync(reviewerUserId, employeeId, ct))
            return ServiceResult<AttendanceCorrectionDto>.Fail("You can't correct your own attendance. Ask another approver.");

        var problem = await ValidateNewAsync(target, dto, ReviewerWindowDays, ct);
        if (problem is not null) return ServiceResult<AttendanceCorrectionDto>.Fail(problem.Message, problem.Type);

        var now = clock.GetUtcNow().UtcDateTime;
        var request = new AttendanceCorrectionRequest
        {
            EmployeeId = employeeId,
            Date = dto.Date!.Value,
            RequestedClockIn = dto.ClockIn,
            RequestedClockOut = dto.ClockOut,
            Reason = dto.Reason.Trim(),
            RequestedByUserId = reviewerUserId,
            Status = ApprovalStatus.Approved,
            ReviewedByUserId = reviewerUserId,
            ReviewedAtUtc = now
        };

        var applyError = await ApplyAsync(request, target, now, ct);
        if (applyError is not null) return ServiceResult<AttendanceCorrectionDto>.Fail(applyError);

        requests.Add(request);
        RecordTimeline(request);
        await unit.Complete();

        return ServiceResult<AttendanceCorrectionDto>.Success(await LoadAsync(request.Id, ct));
    }

    public async Task<ServiceResult<AttendanceCorrectionDto>> CreateForSelfAsync(Guid userId, CreateCorrectionDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<AttendanceCorrectionDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var problem = await ValidateNewAsync(employee, dto, EmployeeWindowDays, ct);
        if (problem is not null) return ServiceResult<AttendanceCorrectionDto>.Fail(problem.Message, problem.Type);

        var request = new AttendanceCorrectionRequest
        {
            EmployeeId = employee.Id,
            Date = dto.Date!.Value,
            RequestedClockIn = dto.ClockIn,
            RequestedClockOut = dto.ClockOut,
            Reason = dto.Reason.Trim(),
            RequestedByUserId = userId
        };

        requests.Add(request);
        await unit.Complete();

        return ServiceResult<AttendanceCorrectionDto>.Success(await LoadAsync(request.Id, ct));
    }

    public async Task<ServiceResult<IReadOnlyList<AttendanceCorrectionDto>>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null)
            return ServiceResult<IReadOnlyList<AttendanceCorrectionDto>>.Fail(NotLinked, ServiceErrorType.NotFound);

        var rows = await requests.ListAsync(new CorrectionQuerySpecification(null, employee.Id, 0, 100), ct);
        return ServiceResult<IReadOnlyList<AttendanceCorrectionDto>>.Success(rows.Select(ToDto).ToList());
    }

    public async Task<PagedResult<AttendanceCorrectionDto>> QueryAsync(ApprovalStatus? status, Guid? employeeId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await requests.CountAsync(new CorrectionQuerySpecification(status, employeeId), ct);
        var rows = await requests.ListAsync(new CorrectionQuerySpecification(status, employeeId, (page - 1) * pageSize, pageSize), ct);

        return new PagedResult<AttendanceCorrectionDto>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<ServiceResult<AttendanceCorrectionDto>> RejectAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note))
            return ServiceResult<AttendanceCorrectionDto>.Fail("A reason is required when rejecting a request.");

        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<AttendanceCorrectionDto>.Fail("Correction request not found.", ServiceErrorType.NotFound);
        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<AttendanceCorrectionDto>.Fail("Only a pending request can be rejected.", ServiceErrorType.Conflict);

        if (await IsOwnAsync(reviewerUserId, request.EmployeeId, ct))
            return ServiceResult<AttendanceCorrectionDto>.Fail("You can't review your own request. Ask another approver.");

        request.Status = ApprovalStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        request.ReviewNote = note.Trim();

        await unit.Complete();
        return ServiceResult<AttendanceCorrectionDto>.Success(await LoadAsync(id, ct));
    }

    private async Task<Problem?> ValidateNewAsync(Employee employee, CreateCorrectionDto dto, int windowDays, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var zoneId = await zones.ResolveAsync(employee, ct);
        var today = LocalClock.LocalDate(now, zoneId);
        var date = dto.Date!.Value;

        if (date > today) return new("You can't correct a future date.");
        if (today.DayNumber - date.DayNumber > windowDays) return new($"Corrections can only go back {windowDays} days.");

        if (await requests.CountAsync(new PendingCorrectionByEmployeeDateSpecification(employee.Id, date), ct) > 0)
            return new("A correction for that day is already pending.", ServiceErrorType.Conflict);

        var existing = await records.GetEntityWithSpec(new AttendanceByEmployeeAndDateSpecification(employee.Id, date), ct);
        var plan = CorrectionPlanner.Plan(date, zoneId, existing?.ClockInUtc, existing?.ClockOutUtc, dto.ClockIn, dto.ClockOut, now);
        return plan.Error is null ? null : new(plan.Error);
    }

    private async Task<string?> ApplyAsync(AttendanceCorrectionRequest request, Employee employee, DateTime now, CancellationToken ct)
    {
        var zoneId = await zones.ResolveAsync(employee, ct);
        var existing = await records.GetEntityWithSpec(new AttendanceByEmployeeAndDateSpecification(employee.Id, request.Date), ct);

        var plan = CorrectionPlanner.Plan(
            request.Date,
            zoneId,
            existing?.ClockInUtc,
            existing?.ClockOutUtc,
            request.RequestedClockIn,
            request.RequestedClockOut,
            now);
        if (plan.Error is not null) return plan.Error;

        var flags = ((existing?.Flags ?? AttendanceFlags.None) & ~AttendanceFlags.LongShift) | AttendanceFlags.ManuallyCorrected;
        if (plan.ClockOutUtc is DateTime outUtc && outUtc - plan.ClockOutUtc > LongShift)
            flags |= AttendanceFlags.LongShift;

        if (existing is null)
        {
            var schedule = employee.ScheduleId is Guid sid ? await schedules.GetByIdAsync(sid, ct) : null;
            records.Add(new AttendanceRecord
            {
                EmployeeId = employee.Id,
                Date = request.Date,
                ClockInUtc = plan.ClockInUtc,
                ClockOutUtc = plan.ClockOutUtc,
                ScheduledStart = schedule?.StartTime,
                ScheduleEnd = schedule?.EndTime,
                Flags = flags
            });
        }
        else
        {
            existing.ClockInUtc = plan.ClockInUtc;
            existing.ClockOutUtc = plan.ClockOutUtc;
            existing.Flags = flags;
        }

        return null;
    }

    private void RecordTimeline(AttendanceCorrectionRequest request) =>
        timeline.Record(TimelineSubjectType.Employee, request.EmployeeId, TimeLineEventType.AttendanceCorrected,
        $"Attendance corrected for {request.Date:yyyy-MM-dd}", request.Reason);

    private async Task<bool> IsOwnAsync(Guid reviewerUserId, Guid employeeId, CancellationToken ct) =>
        (await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(reviewerUserId), ct))?.Id == employeeId;

    private async Task<AttendanceCorrectionDto> LoadAsync(Guid id, CancellationToken ct) =>
        ToDto((await requests.GetEntityWithSpec(new CorrectionByIdWithEmployeeSpecification(id), ct))!);

    private static AttendanceCorrectionDto ToDto(AttendanceCorrectionRequest r) => new(
        r.Id,
        r.EmployeeId,
        r.Employee.EmployeeNumber,
        $"{r.Employee.FirstName} {r.Employee.LastName}",
        r.Date,
        r.RequestedClockIn,
        r.RequestedClockOut,
        r.Reason,
        (int)r.Status,
        r.ReviewedByUserId is not null && r.RequestedByUserId == r.RequestedByUserId,
        r.CreatedAtUtc,
        r.ReviewedAtUtc,
        r.ReviewNote);
}