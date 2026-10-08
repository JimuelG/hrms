using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Onboarding;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class OnboardingTaskTemplatesController(IOnboardingTaskTemplateService service) : BaseApiController
{
    [HasPermission(Permissions.Onboarding.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HasPermission(Permissions.Onboarding.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var template = await service.GetByIdAsync(id, ct);
        return template is null ? NotFound() : Ok(template);
    }

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateOnboardingTaskTemplateDto dto, CancellationToken ct) =>
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOnboardingTaskTemplateDto dto, CancellationToken ct) =>
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);
}