using API.Authorization;
using Core.Constant;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class AlertsController(IAlertService service) : BaseApiController
{
    [HasPermission(Permissions.Alerts.View)]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Ok(await service.ListOpenAsync(ct));
    }

    [HasPermission(Permissions.Alerts.Manage)]
    [HttpPost("scan")]
    public async Task<IActionResult> Scan(CancellationToken ct) =>
        Ok(new { changed = await service.ScanAsync(ct) });
}