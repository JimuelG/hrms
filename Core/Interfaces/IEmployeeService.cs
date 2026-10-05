using Core.Common;
using Core.DTOs.Employees;
using Core.DTOs.Organization;
using Core.Entities;

namespace Core.Interfaces;
public interface IEmployeeService
{
    Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(string? search, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeSummaryDto>> GetEligibleManagersAsync(Guid? excludeEmployeeId, CancellationToken ct = default);
}