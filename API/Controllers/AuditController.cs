using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Audit;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class AuditController(IAuditLogQuery audit) : BaseApiController
{
    [HasPermission(Permissions.Audit.View)]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] AuditQuery query, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await audit.SearchAsync(query, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Audit.View)]
    [HttpGet("facets")]
    public async Task<IActionResult> Facets(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Ok(await audit.FacetsAsync(ct));
    }
}