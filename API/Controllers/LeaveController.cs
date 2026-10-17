using API.Authorization;
using API.Extensions;
using Core.Common;
using Core.Constant;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers;
public class LeaveController(
    ILeaveCalculationService calculation, 
    ILeaveBalanceService balances,
    ILeaveRequestService requests,
    ILeaveAttachmentService attachments) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] LeavePreviewQuery query, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return (await calculation.PreviewAsync(userId, query, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("balances/me")]
    public async Task<IActionResult> GetMyBalances([FromQuery] int? year, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await balances.GetMineAsync(userId, year, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("balances/me/ledger")]
    public async Task<IActionResult> GetMyLedger([FromQuery] int? year, [FromQuery] Guid? leaveTypeId, CancellationToken ct)
    {
        if(!TryGetUserId(out var userId)) return Unauthorized();
        return (await balances.GetMyLedgerAsync(userId, year, leaveTypeId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Manage)]
    [HttpGet("balances/employee/{employeeId:guid}")]
    public async Task<IActionResult> GetEmployeeBalances(Guid employeeId, [FromQuery] int? year, CancellationToken ct) =>
        (await balances.GetForEmployeeAsync(employeeId, year, ct)).ToActionResult(this);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpPost("balances/grant")]
    public async Task<IActionResult> Grant(PostLedgerEntryDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await balances.GrantAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Leave.Manage)]
    [HttpPost("balances/adjust")]
    public async Task<IActionResult> Adjust(PostLedgerEntryDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await balances.AdjustAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpPost("requests")]
    public async Task<IActionResult> File(CreateLeaveRequestDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await requests.FileAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("requests/me")]
    public async Task<IActionResult> GetMyRequests([FromQuery] ApprovalStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken  ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await requests.GetMineAsync(userId, status, page, pageSize, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await requests.GetAsync(userId, id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Manage)]
    [HttpGet("requests")]
    public async Task<IActionResult> QueryRequests([FromQuery] ApprovalStatus? status, [FromQuery] Guid? employeeId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        (await requests.QueryAsync(status, employeeId, from, to, page, pageSize, ct)).ToActionResult(this);

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("requests/{id:guid}/documents")]
    public async Task<IActionResult> ListDocuments(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await attachments.ListAsync(userId, id, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpPost("requests/{id:guid}/documents")]
    [RequestSizeLimit(LeaveAttachmentRules.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadDocument(Guid id, IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await using var stream = file.OpenReadStream();
        return (await attachments.UploadAsync(userId, id, file.FileName, stream, file.Length, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("requests/{id:guid}/documents/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid attachmentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await attachments.DownloadAsync(userId, id, attachmentId, ct);
        if (!result.Succeeded) return result.ToActionResult(this);

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        var f = result.Value!;
        return File(f.Content, f.ContentType, f.FileName);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpDelete("requests/{id:guid}/documents/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid attachmentId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await attachments.DeleteAsync(userId, id, attachmentId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Leave.Request)]
    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelLeaveRequestDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await requests.CancelAsync(userId, id, dto?.Reason, ct)).ToActionResult(this);
    }
}