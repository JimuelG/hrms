using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Employees;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class IdentifiersController(IIdentifierService service) : BaseApiController
{
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
    private void NoStore() => Response.Headers.CacheControl = "private, no-store";

    [HasPermission(Permissions.Identifiers.Read)]
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken ct)
    {
        NoStore();
        return (await service.GetAsync(employeeId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Identifiers.Write)]
    [HttpPut("employee/{employeeId:guid}")]
    public async Task<IActionResult> Set(Guid employeeId, UpdateIdentifiersDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        NoStore();
        return (await service.SetAsync(userId, employeeId, dto, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Identifiers.Reveal)]
    [HttpPost("employee/{employeeId:guid}/reveal")]
    public async Task<IActionResult> Reveal(Guid employeeId, RevealDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        NoStore();
        return (await service.RevealAsync(userId, employeeId, dto.Field, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Identifiers.Reveal)]
    [HttpGet("employee/{employeeId:guid}/access-log")]
    public async Task<IActionResult> AccessLog(Guid employeeId, CancellationToken ct)
    {
        NoStore();
        return (await service.AccessLogAsync(employeeId, ct)).ToActionResult(this);
    }
}