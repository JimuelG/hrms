using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Recruitment;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class InterviewsController(IInterviewService service) : BaseApiController
{
    [HasPermission(Permissions.Interviews.Read)]
    [HttpGet("by-application/{applicationId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid applicationId, CancellationToken ct) =>
        Ok(await service.GetByApplicationAsync(applicationId, ct));

    [HasPermission(Permissions.Interviews.Read)]
    [HttpGet("by-interviewer/{interviewerId:guid}")]
    public async Task<IActionResult> GetByInterviewer(Guid interviewerId, [FromQuery] DateTime? from, CancellationToken ct) =>
        Ok(await service.GetByInterviewerAsync(interviewerId, from, ct));

    [HasPermission(Permissions.Interviews.Write)]
    [HttpPost]
    public async Task<IActionResult> Schedule(ScheduleInterviewDto dto, CancellationToken ct) =>
        (await service.ScheduleAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Interviews.Write)]
    [HttpPut("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleInterviewDto dto, CancellationToken ct) =>
        (await service.RescheduleAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Interviews.Write)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelInterviewDto dto, CancellationToken ct) =>
        (await service.CancelAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Interviews.Write)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct) =>
        (await service.CompleteAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Interviews.Write)]
    [HttpPost("{id:guid}/evaluation")]
    public async Task<IActionResult> SubmitEvaluation(Guid id, SubmitEvaluationDto dto, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        return (await service.SubmitEvaluationAsync(id, dto, userId, ct)).ToActionResult(this);
    }
}