using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Leave;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class LeaveTypesController(ILeaveTypeService service) : BaseApiController
{
    [HasPermission(Permissions.Leave.Request)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await service.GetAllAsync(search, activeOnly, ct));

    [HasPermission(Permissions.Leave.Request)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var type = await service.GetByIdAsync(id, ct);
        return type is null ? NotFound() : Ok(type);
    }
 
    [HasPermission(Permissions.Leave.Manage)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveTypeDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateLeaveTypeDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}