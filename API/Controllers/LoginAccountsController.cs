using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Accounts;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;
public class LoginAccountsController(IUserAccountService accounts) : BaseApiController
{
    private void NoStore() => Response.Headers.CacheControl = "private, no-store";

    [HasPermission(Permissions.Users.Manage)]
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken ct)
    {
        NoStore();
        return (await accounts.GetAsync(employeeId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Users.Manage)]
    [EnableRateLimiting("sensitive")]
    [HttpPost("employee/{employeeId:guid}")]
    public async Task<IActionResult> Create(Guid employeeId, CreateLoginDto dto, CancellationToken ct)
    {
        NoStore();
        return (await accounts.CreateAsync(employeeId, dto.Email, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Users.Manage)]
    [EnableRateLimiting("sensitive")]
    [HttpPost("employee/{employeeId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid employeeId, CancellationToken ct)
    {
        NoStore();
        return (await accounts.ResetPasswordAsync(employeeId, ct)).ToActionResult(this);
    }
}