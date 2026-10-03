using API.Authorization;
using API.Extensions;
using Core.DTOs.Platform;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v1/platform/tenants")]
[PlatformAdminOnly]
public class PlatformTenantsController(IPlatformTenantService service)
    : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) 
        => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await service.GetByIdAsync(id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantAdminDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct) =>
        (await service.SuspendAsync(id, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct) =>
        (await service.ReactivateAsync(id, ct)).ToActionResult(this);
    
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromServices] IPlatformSubscriptionService subs, CancellationToken ct) =>
        Ok(await subs.GetPlansAsync(ct));

    [HttpPost("{id:guid}/plan")]
    public async Task<IActionResult> AssignPlan(Guid id, AssignPlanDto dto, [FromServices] IPlatformSubscriptionService subs, CancellationToken ct) =>
        (await subs.AssignPlanAsync(id, dto.SubscriptionPlanId, ct)).ToActionResult(this);
}