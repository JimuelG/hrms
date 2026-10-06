using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Recruitment;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class JobOffersController(IJobOfferService service) : BaseApiController
{
    [HasPermission(Permissions.JobOffers.Read)]
    [HttpGet("by-application/{applicationId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid applicationId, CancellationToken ct) =>
        Ok(await service.GetByApplicationAsync(applicationId, ct));

    [HasPermission(Permissions.JobOffers.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateJobOfferDto dto, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        return (await service.CreateAsync(dto, userId, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.JobOffers.Write)]
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct) =>
        (await service.SendAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.JobOffers.Write)]
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) =>
        (await service.AcceptAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.JobOffers.Write)]
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, DeclineJobOfferDto dto, CancellationToken ct) =>
        (await service.DeclineAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.JobOffers.Write)]
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct) =>
        (await service.WithdrawnAsync(id, ct)).ToActionResult(this);
}