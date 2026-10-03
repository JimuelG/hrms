using Core.Common;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IEmergencyContactService
{
    Task<IReadOnlyList<EmergencyContactDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<EmergencyContactDto>> CreateAsync(Guid employeeId, UpsertEmergencyContactDto dto, CancellationToken ct = default);
    Task<ServiceResult<EmergencyContactDto>> UpdateAsync(Guid employeeId, Guid contractId, UpsertEmergencyContactDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid employeeId, Guid contactId, CancellationToken ct = default);
}