using Core.Common;
using Core.DTOs.Onboarding;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Onboarding;

public sealed class OnboardingTaskTemplateService(
    IUnitOfWork unit,
    IGenericRepository<OnboardingTaskTemplate> repo) : IOnboardingTaskTemplateService
{
    public async Task<ServiceResult<OnboardingTaskTemplateDto>> CreateAsync(CreateOnboardingTaskTemplateDto dto, CancellationToken ct = default)
    {
        var template = new OnboardingTaskTemplate
        {
            Title = dto.Title,
            Description = dto.Description,
            IsRequired = dto.IsRequired,
            SortOrder = dto.SortOrder
        };

        repo.Add(template);
        await unit.Complete();

        return ServiceResult<OnboardingTaskTemplateDto>.Success(ToDto(template));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await repo.GetByIdAsync(id, ct);
        if (template is null) return ServiceResult<bool>.Fail("task template not found.", ServiceErrorType.NotFound);

        repo.Remove(template);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<OnboardingTaskTemplateDto>> GetAllAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(new ActiveTaskTemplatesSpecification(), ct)).Select(ToDto).ToList();

    public async Task<OnboardingTaskTemplateDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await repo.GetByIdAsync(id, ct);
        return template is null ? null : ToDto(template);
    }

    public async Task<ServiceResult<OnboardingTaskTemplateDto>> UpdateAsync(Guid id, UpdateOnboardingTaskTemplateDto dto, CancellationToken ct = default)
    {
        var template = await repo.GetByIdAsync(id, ct);
        if (template is null) return ServiceResult<OnboardingTaskTemplateDto>.Fail("Task template not found.", ServiceErrorType.NotFound);

        template.Title = dto.Title;
        template.Description = dto.Description;
        template.IsRequired = dto.IsRequired;
        template.SortOrder = dto.SortOrder;
        template.IsActive = dto.IsActive;

        await unit.Complete();

        return ServiceResult<OnboardingTaskTemplateDto>.Success(ToDto(template));
    }

    private static OnboardingTaskTemplateDto ToDto(OnboardingTaskTemplate t) => new(
        t.Id,
        t.Title,
        t.Description,
        t.IsRequired,
        t.SortOrder,
        t.IsActive);
}