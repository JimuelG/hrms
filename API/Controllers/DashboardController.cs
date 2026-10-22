using API.Extensions;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class DashboardController(IDashboardService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        return (await service.GetAsync(userId, ct)).ToActionResult(this);
    }
}