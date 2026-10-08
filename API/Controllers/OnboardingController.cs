using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Onboarding;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class OnboardingController(IOnboardingService service) : BaseApiController
{
    [HasPermission(Permissions.Onboarding.Read)]
    [HttpGet("by-application/{applicationId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid applicationId, CancellationToken ct)
    {
        var result = await service.GetByApplicationAsync(applicationId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpPost]
    public async Task<IActionResult> Start(StartOnboardingDto dto, CancellationToken ct) =>
        (await service.StartAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpPost("{caseId:guid}/tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid caseId, Guid taskId, CancellationToken ct)
    {
        var  userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        return (await service.CompleteTaskAsync(caseId, taskId, userId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Onboarding.Write)]
    [HttpPost("{caseId:guid}/tasks/{taskId:guid}/reopen")]
    public async Task<IActionResult> ReopenTask(Guid caseId, Guid taskId, CancellationToken ct) =>
        (await service.ReopenTaskAsync(caseId, taskId, ct)).ToActionResult(this);

    [HasPermission(Permissions.Onboarding.Write)]
    [HasPermission(Permissions.Employees.Write)]
    [HttpPost("{caseId:guid}/convert")]
    public async Task<IActionResult> Convert(Guid caseId, ConvertToEmloyeeDto dto, CancellationToken ct) =>
        (await service.ConvertToEmployeeAsync(caseId, dto, ct)).ToActionResult(this);
    

}