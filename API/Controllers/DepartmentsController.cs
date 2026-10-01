using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Organization;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class DepartmentsController(IDepartmentService service) : BaseApiController
{
    [HasPermission(Permissions.Departments.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.Departments.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var department = await service.GetByIdAsync(id, ct);
        return department is null ? NotFound() : Ok(department);
    }

    [HasPermission(Permissions.Departments.Write)]
    [HttpPost]
    public async Task<IActionResult> Craete(CreateDepartmentDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Departments.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Departments.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

}