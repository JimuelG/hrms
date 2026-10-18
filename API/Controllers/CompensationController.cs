using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class CompensationController(ICompensationService service) : BaseApiController
{
    [HasPermission(Permissions.Compensation.Read)]
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetHistory(Guid employeeId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await service.GetHistoryAsync(employeeId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Compensation.Write)]
    [HttpPost("employee/{employeeId:guid}")]
    public async Task<IActionResult> Set(Guid employeeId, SetCompensationDto dto, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return (await service.SetAsync(userId, employeeId, dto, ct)).ToActionResult(this, isCreate: true);
    }
}