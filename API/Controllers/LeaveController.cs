using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Leave;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class LeaveController(ILeaveCalculationService service) : BaseApiController
{
    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] LeavePreviewQuery query, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return (await service.PreviewAsync(userId, query, ct)).ToActionResult(this);
    }
}