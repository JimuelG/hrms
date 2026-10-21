using API.Authorization;
using API.Extensions;
using Core.Common;
using Core.Constant;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers;
public class PayrollRunsController(IPayrollRunService service) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    [HasPermission(Permissions.Payroll.Read)]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Ok(await service.ListAsync(ct));
    }

    [HasPermission(Permissions.Payroll.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await service.GetAsync(id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost]
    public async Task<IActionResult> Start(StartRunDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.StartAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("{id:guid}/recompute")]
    public async Task<IActionResult> Recompute(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.RecomputeAsync(userId, id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Read)]
    [HttpGet("{id:guid}/adjustments")]
    public async Task<IActionResult> Adjusments(Guid id, CancellationToken ct) => 
        (await service.GetAdjustmentsAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("{id:guid}/adjustments")]
    public async Task<IActionResult> AddAdjustment(Guid id, AddAdjustmentDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.AddAdjustmentAsync(userId, id, dto, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpDelete("{id:guid}/adjustments/{adjustmentId:guid}")]
    public async Task<IActionResult> RemoveAdjustment(Guid id, Guid adjustmentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.RemoveAdjustmentAsync(userId, id, adjustmentId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Approve)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.ApproveAsync(userId, id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Approve)]
    [HttpPost("{id:guid}/send-back")]
    public async Task<IActionResult> SendBack(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReasonDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.SendBackAsync(userId, id, dto?.Reason, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Finalize)]
    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.FinalizeAsync(userId, id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReasonDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.CancelAsync(userId, id, dto?.Reason, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Read)]
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct) => 
        Ok(await service.GetSettingsAsync(ct));

    [HasPermission(Permissions.Payroll.ManageSettings)]
    public async Task<IActionResult> UpdateSettings(UpdatePayrollSettingsDto dto, CancellationToken ct) =>
        (await service.UpdateSettingsAsync(dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("thirteenth-month")]
    public async Task<IActionResult> StartThirteenth(StartThirteenthDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await service.StartThirteenthAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }
}