using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;

namespace Core.Interfaces;
public interface IInterviewService
{
    Task<ServiceResult<InterviewDto>> ScheduleAsync(ScheduleInterviewDto dto, CancellationToken ct = default);
    Task<ServiceResult<InterviewDto>> RescheduleAsync(Guid id, RescheduleInterviewDto dto, CancellationToken  ct = default);
    Task<ServiceResult<InterviewDto>> CancelAsync(Guid id, CancelInterviewDto dto, CancellationToken ct = default);
    Task<ServiceResult<InterviewDto>> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<InterviewDto>> SubmitEvaluationAsync(Guid id, SubmitEvaluationDto dto, Guid submittedByUserId, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewDto>> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewDto>> GetByInterviewerAsync(Guid interviewerId, DateTime? fromUtc, CancellationToken ct = default);
}