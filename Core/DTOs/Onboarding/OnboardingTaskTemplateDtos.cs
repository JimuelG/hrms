using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Onboarding;

public sealed record OnboardingTaskTemplateDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsRequired,
    int SortOrder,
    bool IsActive);
public class CreateOnboardingTaskTemplateDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";
    [MaxLength(1000)]
    public string? Description { get; init; }
    public bool IsRequired { get; init; } = true;
    public int SortOrder { get; init; }
}

public class UpdateOnboardingTaskTemplateDto : CreateOnboardingTaskTemplateDto
{
    public bool IsActive { get; init; } = true;
}