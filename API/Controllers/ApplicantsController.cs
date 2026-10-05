using API.Authorization;
using API.Extensions;
using Application.Features.Employees;
using Core.Constant;
using Core.DTOs.Employees;
using Core.DTOs.Recruitment;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class ApplicantsController(IApplicantService service, IUnitOfWork unit)
    : BaseApiController
{
    [HasPermission(Permissions.Applicants.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.Applicants.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var applicant = await service.GetByIdAsync(id, ct);
        return applicant is null ? NotFound() : Ok(applicant);
    }

    [HasPermission(Permissions.Applicants.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicantDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Applicants.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateApplicantDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Applicants.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Applicants.Write)]
    [HttpPost("{id:guid}/resume")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadResume(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { message = "No file was uploaded." });
        await using var stream = file.OpenReadStream();
        var result = await service.UploadResumeAsync(id, stream, file.FileName, file.ContentType, file.Length, ct);
        return result.ToActionResult(this);
    }

    [HasPermission(Permissions.Applicants.Read)]
    [HttpGet("{id:guid}/resume")]
    public async Task<IActionResult> DownloadResume(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadResumeAsync(id, ct);
        if (!result.Succeeded) return result.ToActionResult(this);

        var (content, contentType, fileName) = result.Value;

        return File(content, contentType, fileName);
    }

    [HasPermission(Permissions.Applicants.Read)]
    [HttpGet("{applicantId:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid applicantId, [FromServices] ITimelineService timeline, CancellationToken ct) =>
        Ok(await timeline.GetForEmployeeAsync(TimelineSubjectType.Applicant, applicantId, ct));

    [HasPermission(Permissions.Applicants.Write)]
    [HttpPost("{applicantId:guid}/timeline/notes")]
    public async Task<IActionResult> AddTimelineNote(Guid applicantId, AddTimelineNoteDto dto, [FromServices] ITimelineService timeline)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;

        Guid.TryParse(userIdClaim, out var userId);
        timeline.Record(
            TimelineSubjectType.Applicant, 
            applicantId, 
            TimeLineEventType.Note, 
            dto.Title, 
            dto.Description, 
            dto.EventDateUtc, 
            userId == Guid.Empty ? null : userId);

        await unit.Complete();

        return StatusCode(201);
    }
}