using Core.Common;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IEmployeeUserLinkService
{
    Task<ServiceResult<EmployeeDto>> LinkAsync(Guid employeeId, string email, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDto>> UnLinkAsync(Guid employeeId, CancellationToken ct = default);
}