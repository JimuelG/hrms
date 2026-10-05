using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Recruitment;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class JobPostingsController(IJobPostingService service) : BaseApiController
{
    [HasPermission(Permissions.JobPosting.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] JobPostingStatus? status, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, status, ct));

    [HasPermission(Permissions.JobPosting.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var posting = await service.GetByIdAsync(id, ct);
        return posting is null ? NotFound() : Ok(posting);
    }

    [HasPermission(Permissions.JobPosting.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateJobPostingDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.JobPosting.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateJobPostingDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.JobPosting.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}