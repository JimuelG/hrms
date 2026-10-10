using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Attendance;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class WorkLocationsController(IWorkLocationService service) : BaseApiController
{
    [HasPermission(Permissions.WorkLocations.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? type, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, type, ct));

    [HasPermission(Permissions.WorkLocations.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var location = await service.GetByIdAsync(id, ct);
        return location is null ? NotFound() : Ok(location);
    }

    [HasPermission(Permissions.WorkLocations.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkLocationDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.WorkLocations.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkLocationDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.WorkLocations.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delet(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}