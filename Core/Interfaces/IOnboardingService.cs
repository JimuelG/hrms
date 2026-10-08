using Core.Common;
using Core.DTOs.Onboarding;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IOnboardingService
{
    Task<ServiceResult<OnboardingCaseDto>> StartAsync(StartOnboardingDto dto, CancellationToken ct = default);
    Task<ServiceResult<OnboardingCaseDto>> CompleteTaskAsync(Guid caseId, Guid taskId, Guid completedByUserId, CancellationToken ct = default);
    Task<ServiceResult<OnboardingCaseDto>> ReopenTaskAsync(Guid caseId, Guid taskId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDto>> ConvertToEmployeeAsync(Guid caseId, ConvertToEmloyeeDto dto, CancellationToken ct = default);
    Task<OnboardingCaseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OnboardingCaseDto?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
}