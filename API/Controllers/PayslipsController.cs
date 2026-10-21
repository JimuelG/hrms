using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class PayslipsController(IPayslipService service) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    [HasPermission(Permissions.Payroll.ViewOwn)]
    [HttpGet("me")]
    public async Task<IActionResult> ListMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        return (await service.ListMineAsync(userId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.ViewOwn)]
    [HttpGet("me/{runId:guid}")]
    public async Task<IActionResult> GetMine(Guid runId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        return (await service.GetMineAsync(userId, runId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Read)]
    [HttpGet("run/{runId:guid}/employee/{employeeId:guid}")]
    public async Task<IActionResult> GetForEmployee(Guid runId, Guid employeeId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await service.GetForEmployeeAsync(runId, employeeId, ct)).ToActionResult(this);
    }
}