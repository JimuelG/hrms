using API.Authorization;
using Core.Constant;
using Core.DTOs.Organization;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class TenantSettingsController(ITenantSettingsService service) : BaseApiController
{
    [HasPermission(Permissions.Tenant.ManageSettings)]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await service.GetAsync(ct));


    [HasPermission(Permissions.Tenant.ManageRoles)]
    [HttpPut]
    public async Task<IActionResult> Update(UpdateTenantSettingsDto dto, CancellationToken ct) =>
        Ok(await service.UpdateAsync(dto, ct));
}