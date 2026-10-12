using System.Net;
using Core.Common;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Core.DTOs.Attendance;

public class OvertimeService(
    IUnitOfWork unit,
    IGenericRepository<OvertimeRequest> requests,
    IGenericRepository<Employee> employees,
    ITimeZoneResolver zones,
    TimeProvider clock) : IOvertimeService
{
    private const string NotLinked = "No employee profile us linked to your account. Ask HR to link it.";
    private const int DaysBack = 31;
    private const int DaysAhead = 14;

    public async Task<ServiceResult<OvertimeRequestDto>> ApproveAsync(Guid reviewerUserId, Guid id, ApproveOvertimeDto dto, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<OvertimeRequestDto>.Fail("Overtime request not found.", ServiceErrorType.NotFound);
        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<OvertimeRequestDto>.Fail("Only a pending request can be approved.", ServiceErrorType.Conflict);

        if (await IsOwnAsync(reviewerUserId, request.EmployeeId, ct))
            return ServiceResult<OvertimeRequestDto>.Fail("You can't review your own request. Ask another approver.");

        var approved = dto.ApprovedMinutes ?? request.RequestedMinutes;
        if (approved < 1 || approved > request.RequestedMinutes)
            return ServiceResult<OvertimeRequestDto>.Fail($"Approved minutes must between 1 and {request.RequestedMinutes}.");

        request.Status = ApprovalStatus.Approved;
        request.ApprovedMinutes = approved;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        request.ReviewNote = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        await unit.Complete();
        return ServiceResult<OvertimeRequestDto>.Success(await LoadAsync(id, ct));
    }

    public async Task<ServiceResult<OvertimeRequestDto>> CancelAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<OvertimeRequestDto>.Fail(NotLinked, ServiceErrorType.NotFound);
        
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null || request.EmployeeId != employee.Id)
            return ServiceResult<OvertimeRequestDto>.Fail("Overtime request not found.", ServiceErrorType.Conflict);

        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<OvertimeRequestDto>.Fail("Only a pending request can be cancelled.", ServiceErrorType.Conflict);

        request.Status = ApprovalStatus.Cancelled;
        await unit.Complete();
        return ServiceResult<OvertimeRequestDto>.Success(await LoadAsync(id, ct));
    }

    public async Task<ServiceResult<OvertimeRequestDto>> CreateAsync(Guid userId, CreateOvertimeDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<OvertimeRequestDto>.Fail(NotLinked, ServiceErrorType.NotFound);

        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await zones.ResolveAsync(employee, ct));
        var date = dto.Date!.Value;
        var start = dto.StartTime!.Value;
        var end = dto.EndTime!.Value;

        if (today.DayNumber - date.DayNumber > DaysBack)
            return ServiceResult<OvertimeRequestDto>.Fail($"Overtime can only be requested for the lat {DaysBack} days.");
        if (date.DayNumber - today.DayNumber > DaysAhead)
            return ServiceResult<OvertimeRequestDto>.Fail($"Overtime can only be requested up to {DaysAhead} days ahead.");

        var measured = OvertimeRules.Measure(start, end);
        if (measured.Error is not null) return ServiceResult<OvertimeRequestDto>.Fail(measured.Error);

        var sameDay = await requests.ListAsync(new ActiveOvertimeByEmployeeDateSpecification(employee.Id, date), ct);
        if (sameDay.Any(o => OvertimeRules.Overlaps(o.StartTime, o.EndTime, start, end)))
            return ServiceResult<OvertimeRequestDto>.Fail("That overlaps another overtime request for the same day.", ServiceErrorType.Conflict);

        var request = new OvertimeRequest
        {
            EmployeeId = employee.Id,
            Date = date,
            StartTime =start,
            EndTime = end,
            RequestedMinutes = measured.Minutes,
            Reason = dto.Reason.Trim()
        };

        requests.Add(request);
        await unit.Complete();

        return ServiceResult<OvertimeRequestDto>.Success(await LoadAsync(request.Id, ct));
    }

    public async Task<ServiceResult<IReadOnlyList<OvertimeRequestDto>>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null)
            return ServiceResult<IReadOnlyList<OvertimeRequestDto>>.Fail(NotLinked, ServiceErrorType.NotFound);
        
        var rows = await requests.ListAsync(new OvertimeQuerySpecification(null, employee.Id, 0, 100), ct);
        return ServiceResult<IReadOnlyList<OvertimeRequestDto>>.Success(rows.Select(ToDto).ToList());
    }

    public async Task<PagedResult<OvertimeRequestDto>> QueryAsync(ApprovalStatus? status, Guid? employeeId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await requests.CountAsync(new OvertimeQuerySpecification(status, employeeId), ct);
        var rows = await requests.ListAsync(new OvertimeQuerySpecification(status, employeeId, (page - 1) * pageSize), ct);
        return new PagedResult<OvertimeRequestDto>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<ServiceResult<OvertimeRequestDto>> RejectAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note))
            return ServiceResult<OvertimeRequestDto>.Fail("A reason is required when rejecting a request.");

        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<OvertimeRequestDto>.Fail("Overtime request not found.", ServiceErrorType.NotFound);
        if (request.Status != ApprovalStatus.Pending)
            return ServiceResult<OvertimeRequestDto>.Fail("Only a pending request can be rejected.", ServiceErrorType.Conflict);

        if (await IsOwnAsync(reviewerUserId, request.EmployeeId, ct))
            return ServiceResult<OvertimeRequestDto>.Fail("You can't review your own request. Ask another approver.");

        request.Status = ApprovalStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        request.ReviewNote = note.Trim();

        await unit.Complete();
        return ServiceResult<OvertimeRequestDto>.Success(await LoadAsync(id, ct));
    }

    private async Task<bool> IsOwnAsync(Guid reviewerUserId, Guid employeeId, CancellationToken ct) =>
        (await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(reviewerUserId), ct))?.Id == employeeId;

    private async Task<OvertimeRequestDto> LoadAsync(Guid id, CancellationToken ct) =>
        ToDto((await requests.GetEntityWithSpec(new OvertimeByIdWithEmployeeSpecification(id), ct))!);

    private static OvertimeRequestDto ToDto(OvertimeRequest r) => new(
        r.Id,
        r.EmployeeId,
        r.Employee.EmployeeNumber,
        $"{r.Employee.FirstName} {r.Employee.LastName}",
        r.Date,
        r.StartTime,
        r.EndTime,
        r.RequestedMinutes,
        r.ApprovedMinutes,
        r.Reason,
        (int)r.Status,
        r.CreatedAtUtc,
        r.ReviewedAtUtc,
        r.ReviewNote
    );
}