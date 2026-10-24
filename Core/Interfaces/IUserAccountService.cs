using Core.Common;
using Core.DTOs.Accounts;

namespace Core.Interfaces;
public interface IUserAccountService
{
    Task<ServiceResult<LoginAccountDto>> GetAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<IssuedLoginDto>> CreateAsync(Guid employeeId, string? email, CancellationToken ct = default);
    Task<ServiceResult<IssuedLoginDto>> ResetPasswordAsync(Guid employeeId, CancellationToken ct = default);
}

public interface ILeaverAccessSweep
{
    Task<int> RunAsync(CancellationToken ct = default);
}