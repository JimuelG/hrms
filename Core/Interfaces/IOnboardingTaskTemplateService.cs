using Core.Common;
using Core.DTOs.Onboarding;

namespace Core.Interfaces;
public interface IOnboardingTaskTemplateService
{
    Task<ServiceResult<OnboardingTaskTemplateDto>> CreateAsync(CreateOnboardingTaskTemplateDto dto, CancellationToken  ct = default);
    Task<ServiceResult<OnboardingTaskTemplateDto>> UpdateAsync(Guid id, UpdateOnboardingTaskTemplateDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<OnboardingTaskTemplateDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OnboardingTaskTemplateDto>> GetAllAsync(CancellationToken ct = default);
}