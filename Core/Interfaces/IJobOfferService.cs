using Core.Common;
using Core.DTOs.Recruitment;

namespace Core.Interfaces;
public interface IJobOfferService
{
    Task<ServiceResult<JobOfferDto>> CreateAsync(CreateJobOfferDto dto, Guid createdByUserId, CancellationToken ct = default);
    Task<ServiceResult<JobOfferDto>> SendAsync(Guid id, CancellationToken ct = default!);
    Task<ServiceResult<JobOfferDto>> AcceptAsync(Guid id, CancellationToken ct = default!);
    Task<ServiceResult<JobOfferDto>> DeclineAsync(Guid id, DeclineJobOfferDto dto, CancellationToken ct = default);
    Task<ServiceResult<JobOfferDto>> WithdrawnAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JobOfferDto>> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
}