using API.Authorization;
using Core.Constant;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;
public class TestController : BaseApiController
{
    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("employees-check")]
    public IActionResult EmployeesCheck() => Ok(new { message = "You have employees.read"});

    [HasPermission(Permissions.Tenant.ManageRoles)]
    [HttpPost("rename-role")]
    public async Task<IActionResult> RenameRole([FromServices] AppDbContext db)
    {
        var role = await db.Roles.FirstAsync();
        role.Name = "HR Admin " + Guid.NewGuid().ToString()[..4];
        await db.SaveChangesAsync();
        return Ok();
    }
}