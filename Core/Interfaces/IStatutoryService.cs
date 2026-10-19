using Core.Common;
using Core.DTOs.Payroll;
using Core.Enums;

namespace Core.Interfaces;
public interface IStatutoryService
{
    Task<IReadOnlyList<RuleSetDto>> ListAsync(StatutoryKind? kind, CancellationToken ct = default);
    Task<ServiceResult<RuleSetDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<RuleSetDto>> CreateAsync(Guid actorUserId, CreateRuleSetDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<InForceRules> LoadInForceAsync(DateOnly date, PayFrequency taxBasis, CancellationToken ct = default);

    Task<ServiceResult<StatutoryPreviewDto>> PreviewAsync(StatutoryPreviewQuery q, CancellationToken ct = default);
}