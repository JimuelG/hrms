using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Payroll;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class PayrollController(IRecurringPayrollService recurring) : BaseApiController
{
    [HasPermission(Permissions.Payroll.Run)]
    [HttpGet("recurring-items/employee/{employeeId:guid}")]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await recurring.ListAsync(employeeId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("recurring-items")]
    public async Task<IActionResult> Create(CreateRecurringItemDto dto, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Unauthorized();
        return (await recurring.CreateAsync(userId, dto, ct)).ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Payroll.Run)]
    [HttpPost("recurring-items/{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        (await recurring.DeactivateAsync(id, ct)).ToActionResult(this);
}