using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class HolidaysController(IHolidayService service) : BaseApiController
{
    [HasPermission(Permissions.Leave.Request)]
    [HttpGet]
    public async Task<IActionResult> GetByYear([FromQuery] int? year, CancellationToken ct) =>
        (await service.GetByYearAsync(year ?? DateTime.UtcNow.Year, ct)).ToActionResult(this);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpPost]
    public async Task<IActionResult> Create(UpsertHolidayDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertHolidayDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Leave.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}