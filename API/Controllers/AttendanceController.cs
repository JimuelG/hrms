using System.ComponentModel.DataAnnotations;
using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Attendance;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class AttendanceController(IAttendanceService service) : BaseApiController
{
    private bool TryGetUsedId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    [HasPermission(Permissions.Attendance.Clock)]
    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn(ClockRequestDto dto, CancellationToken ct)
    {
        if (!TryGetUsedId(out var userId)) return Unauthorized();
        return (await service.ClockInAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Attendance.Clock)]
    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut(ClockRequestDto dto, CancellationToken ct)
    {
        if (!TryGetUsedId(out var userId)) return Unauthorized();
        return (await service.ClockOutAsync(userId, dto, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Attendance.Clock)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        if (!TryGetUsedId(out var userId)) return Unauthorized();
        return (await service.GetMineAsync(userId, from, to, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Attendance.Read)]
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery, Required] DateOnly? from, [FromQuery, Required] DateOnly? to,
        [FromQuery] Guid? employeeId, [FromQuery] bool needsReview = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        (await service.QueryAsync(from!.Value, to!.Value, employeeId, needsReview, page, pageSize, ct)).ToActionResult(this);
}