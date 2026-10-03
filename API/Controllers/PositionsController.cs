using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Organization;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class PositionsController(IPositionService service)
    : BaseApiController
{
    [HasPermission(Permissions.Positions.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.Positions.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var position = await service.GetByIdAsync(id, ct);
        return position is null ? NotFound() : Ok(position);
    }

    [HasPermission(Permissions.Positions.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePositionDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Positions.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePositionDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Positions.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

}