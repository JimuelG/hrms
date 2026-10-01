using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Organization;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class BranchesController(IBranchService service) : BaseApiController
{
    [HasPermission(Permissions.Branches.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok (await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.Branches.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var branch = await service.GetByIdAsync(id, ct);
        return branch is null ? NotFound() : Ok(branch);
    }

    [HasPermission(Permissions.Branches.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBranchDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Branches.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBranchDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Branches.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

}