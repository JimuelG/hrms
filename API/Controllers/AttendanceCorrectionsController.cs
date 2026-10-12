using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Attendance;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers;
public class AttendanceCorrectionsController(IAttendanceCorrectionService service) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    // ---- self-service ----
    [HasPermission(Permissions.Attendance.Clock)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCorrectionDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.CreateForSelfAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Attendance.Clock)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.GetMineAsync(userId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Attendance.Clock)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.CancelAsync(userId, id, ct)).ToActionResult(this);
    }

    // ---- approvers ----
    [HasPermission(Permissions.Attendance.Approve)]
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] ApprovalStatus? status, [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await service.QueryAsync(status, employeeId, page, pageSize, ct));

    [HasPermission(Permissions.Attendance.Approve)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.ApproveAsync(userId, id, dto?.Note, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Attendance.Approve)]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.RejectAsync(userId, id, dto?.Note, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Attendance.Approve)]
    [HttpPost("for-employee/{employeeId:guid}")]
    public async Task<IActionResult> CreateForEmployee(Guid employeeId, CreateCorrectionDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.CreateForEmployeeAsync(userId, employeeId, dto, ct)).ToActionResult(this, isCreate: true);
    }
}