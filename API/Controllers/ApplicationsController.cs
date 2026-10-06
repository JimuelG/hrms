using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Recruitment;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class ApplicationsController(IApplicationService service) : BaseApiController
{
    [HasPermission(Permissions.Applications.Read)]
    [HttpGet("by-posting/{jobPostingId:guid}")]
    public async Task<IActionResult> GetByPosting(Guid jobPostingId, [FromQuery] ApplicationStatus? status, CancellationToken ct) =>
        Ok(await service.GetByPostingAsync(jobPostingId, status, ct));

    [HasPermission(Permissions.Applications.Read)]
    [HttpGet("by-applicant/{applicantId:guid}")]
    public async Task<IActionResult> GetByApplicant(Guid applicantId, CancellationToken ct) =>
        Ok(await service.GetByApplicantAsync(applicantId, ct));

    [HasPermission(Permissions.Applications.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateApplicationDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Applications.Write)]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateApplicationStatusDto dto, CancellationToken ct) =>
        (await service.UpdateStatusAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Applications.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var application = await service.GetByIdAsync(id, ct);
        return application is null ? NotFound() : Ok(application);
    }
}