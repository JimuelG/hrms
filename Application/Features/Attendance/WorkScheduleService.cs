using Core.Common;
using Core.DTOs.Attendance;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Attendance;

public sealed class WorkScheduleService(
    IUnitOfWork unit,
    IGenericRepository<WorkSchedule> repo) : IWorkScheduleService
{
    public async Task<ServiceResult<WorkScheduleDto>> CreateAsync(CreateWorkScheduleDto dto, CancellationToken ct = default)
    {
        if (dto.EndTime <= dto.StartTime)
            return ServiceResult<WorkScheduleDto>.Fail("End time must be after start time.");

        if (await repo.CountAsync(new WorkScheduleByNameSpecification(dto.Name), ct) > 0)
            return ServiceResult<WorkScheduleDto>.Fail($"A schedule named '{dto.Name}' already exists.", ServiceErrorType.Conflict);

        var schedule = new WorkSchedule
        {
            Name = dto.Name,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            WorkingDays = (WorkingDay)dto.WorkingDays
        };

        repo.Add(schedule);
        await unit.Complete();
        return ServiceResult<WorkScheduleDto>.Success(ToDto(schedule));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await repo.GetByIdAsync(id, ct);
        if (schedule is null) return ServiceResult<bool>.Fail("Work schedule not found.", ServiceErrorType.NotFound);

        repo.Remove(schedule);

        await unit.Complete();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<WorkScheduleDto>> GetAllAsync(string? search, CancellationToken ct = default) =>
        (await repo.ListAsync(new WorkScheduleSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<WorkScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await repo.GetByIdAsync(id, ct);

        return schedule is null ? null : ToDto(schedule);
    }

    public async Task<ServiceResult<WorkScheduleDto>> UpdateAsync(Guid id, UpdateWorkScheduleDto dto, CancellationToken ct = default)
    {
        var schedule = await repo.GetByIdAsync(id, ct);
        if (schedule is null) return ServiceResult<WorkScheduleDto>.Fail("Work schedule not found.", ServiceErrorType.NotFound);

        if (dto.EndTime <= dto.StartTime)
            return ServiceResult<WorkScheduleDto>.Fail("End time must be after start time.");

        if (await repo.CountAsync(new WorkScheduleByNameSpecification(dto.Name, excludeId: id), ct) > 0)
            return ServiceResult<WorkScheduleDto>.Fail($"A schedule named '{dto.Name}' already exists.", ServiceErrorType.Conflict);

        schedule.Name = dto.Name;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.WorkingDays = (WorkingDay)dto.WorkingDays;
        schedule.IsActive = dto.IsActive;

        await unit.Complete();
        return ServiceResult<WorkScheduleDto>.Success(ToDto(schedule));
    }

    public static WorkScheduleDto ToDto(WorkSchedule s) => new(
        s.Id,
        s.Name,
        s.StartTime,
        s.EndTime,
        s.WorkingDays,
        s.IsActive,
        s.CreatedAtUtc);
}