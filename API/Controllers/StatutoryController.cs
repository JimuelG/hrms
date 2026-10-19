using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Payroll;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class StatutoryController(IStatutoryService service) : BaseApiController
{
    [HasPermission(Permissions.Payroll.ReadRules)]
    [HttpGet("rule-sets")]
    public async Task<IActionResult> List([FromQuery] StatutoryKind? kind, CancellationToken ct) =>
        Ok(await service.ListAsync(kind, ct));

    [HasPermission(Permissions.Payroll.ReadRules)]
    [HttpGet("rule-sets/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await service.GetAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Payroll.ManageRules)] 
    [HttpPost("rule-sets")]
    public async Task<IActionResult> Create(CreateRuleSetDto dto, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return (await service.CreateAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Payroll.ManageRules)]
    [HttpDelete("rule-sets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Payroll.ReadRules)]
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] StatutoryPreviewQuery query, CancellationToken ct) =>
        (await service.PreviewAsync(query, ct)).ToActionResult(this);
}