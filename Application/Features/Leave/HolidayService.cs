using Core.Common;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class HolidayService(IUnitOfWork unit, IGenericRepository<Holiday> repo) : IHolidayService
{
    public async Task<ServiceResult<HolidayDto>> CreateAsync(UpsertHolidayDto dto, CancellationToken ct = default)
    {
        var error = Validate(dto);
        if (error is not null) return ServiceResult<HolidayDto>.Fail(error);

        var date = dto.Date!.Value;
        if (await repo.CountAsync(new HolidayByDateSpecification(date), ct) > 0)
            return ServiceResult<HolidayDto>.Fail($"{date:yyyy-MM-dd} already has a holiday.", ServiceErrorType.Conflict);

        var holiday = new Holiday { Date = date, Name = dto.Name.Trim(), Type = (HolidayType)dto.Type };

        repo.Add(holiday);
        await unit.Complete();
        return ServiceResult<HolidayDto>.Success(ToDto(holiday));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var holiday = await repo.GetByIdAsync(id, ct);
        if (holiday is null) return ServiceResult<bool>.Fail("Holiday not found.", ServiceErrorType.NotFound);

        repo.Remove(holiday);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyList<HolidayDto>>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        if (year < LeaveCalculator.MinYear || year > LeaveCalculator.MaxYear)
            return ServiceResult<IReadOnlyList<HolidayDto>>.Fail($"Year must be between {LeaveCalculator.MinYear} and {LeaveCalculator.MaxYear}.");

        var rows = await repo.ListAsync(new HolidaysInRangeSpecification(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), ct);
        return ServiceResult<IReadOnlyList<HolidayDto>>.Success(rows.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<HolidayDto>> UpdateAsync(Guid id, UpsertHolidayDto dto, CancellationToken ct = default)
    {
        var holiday = await repo.GetByIdAsync(id, ct);
        if (holiday is null) return ServiceResult<HolidayDto>.Fail("Holiday not found.", ServiceErrorType.NotFound);

        var error = Validate(dto);
        if (error is not null) return ServiceResult<HolidayDto>.Fail(error);

        var date = dto.Date!.Value;
        if (await repo.CountAsync(new HolidayByDateSpecification(date, excludeId: id), ct) > 0)
            return ServiceResult<HolidayDto>.Fail($"{date:yyyy-MM-dd} already has a holiday.", ServiceErrorType.Conflict);

        holiday.Date = date;
        holiday.Name = dto.Name.Trim();
        holiday.Type = (HolidayType)dto.Type;
        await unit.Complete();
        return ServiceResult<HolidayDto>.Success(ToDto(holiday));
    }

    private static string? Validate(UpsertHolidayDto dto)
    {
        var year = dto.Date!.Value.Year;
        if (year < LeaveCalculator.MinYear || year > LeaveCalculator.MaxYear)
            return $"Year must be between {LeaveCalculator.MinYear} and {LeaveCalculator.MaxYear}";

        return Enum.IsDefined(typeof(HolidayType), dto.Type) ? null : "Choose a valid holiday type.";
    }

    private static HolidayDto ToDto(Holiday h) => new(
        h.Id,
        h.Date,
        h.Name,
        (int)h.Type);
}