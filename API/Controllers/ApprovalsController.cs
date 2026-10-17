using API.Extensions;
using Core.DTOs.Approvals;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Controllers;

[Authorize]
public class ApprovalsController(IApprovalQueryService query, IApprovalWorkFlowService workflow) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await query.GetAsync(userId, id, ct)).ToActionResult(this);   
    }

    [HttpGet("by-subject")]
    public async Task<IActionResult> GetBySubject([FromQuery] ApprovalSubjectType subjectType, [FromQuery] Guid subjectId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await query.GetBySubjectAsync(userId, subjectType, subjectId, ct)).ToActionResult(this);
    }
    
    [HttpGet("inbox")]
    public async Task<IActionResult> Inbox([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await query.GetInboxAsync(userId, page, pageSize, ct)).ToActionResult(this);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromQuery] ApprovalStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await query.GetMineAsync(userId, status, page, pageSize, ct)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApprovalNoteDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await workflow.ApprovedAsync(userId, id, dto?.Note, ct)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApprovalNoteDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await workflow.RejectAsync(userId, id, dto?.Note, ct)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> Comment(
        Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApprovalNoteDto? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return (await workflow.CommentAsync(userId, id, dto?.Note, ct)).ToActionResult(this);
    }
}