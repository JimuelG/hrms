using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Attendance;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class WorkSchedulesController(IWorkScheduleService service) : BaseApiController
{
    [HasPermission(Permissions.WorkSchedules.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.WorkSchedules.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var schedule = await service.GetByIdAsync(id, ct);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HasPermission(Permissions.WorkSchedules.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkScheduleDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.WorkSchedules.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkScheduleDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.WorkSchedules.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}