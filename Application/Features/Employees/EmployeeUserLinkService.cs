using Core.Common;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public class EmployeeUserLinkService(
    IUnitOfWork unit,
    IGenericRepository<Employee> employees,
    IUserDirectory users,
    ITenantContext tenant,
    IEmployeeService employeeService) : IEmployeeUserLinkService
{
    public async Task<ServiceResult<EmployeeDto>> LinkAsync(Guid employeeId, string email, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ServiceResult<EmployeeDto>.Fail("Employee not found", ServiceErrorType.NotFound);

        if (employee.UserId is not null)
            return ServiceResult<EmployeeDto>.Fail("This employee is already linked to a login. Unlink it first.", ServiceErrorType.Conflict);

        var userId = await users.FindActiveMemberIdAsync(email.Trim(), tenant.RequiredTenantId, ct);
        if (userId is null)
            return ServiceResult<EmployeeDto>.Fail("No active user with that email belongs to this company.");

        if (await employees.CountAsync(new EmployeeByUserIdSpecification(userId.Value), ct) > 0)
            return ServiceResult<EmployeeDto>.Fail("That login is already linked to another employee.", ServiceErrorType.Conflict);
        
        employee.UserId = userId;
        await unit.Complete();

        return ServiceResult<EmployeeDto>.Success((await employeeService.GetByIdAsync(employeeId, ct))!);
    }

    public async Task<ServiceResult<EmployeeDto>> UnLinkAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ServiceResult<EmployeeDto>.Fail("Employee not found", ServiceErrorType.NotFound);

        employee.UserId = null;
        await unit.Complete();

        return ServiceResult<EmployeeDto>.Success((await employeeService.GetByIdAsync(employeeId, ct))!);
    }
}