using Core.Common;
using Core.DTOs.Attendance;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.VisualBasic;

namespace Application.Features.Attendance;

public class WorkLocationService(
    IUnitOfWork unit,
    IGenericRepository<WorkLocation> repo,
    IGenericRepository<Branch> branches) : IWorkLocationService
{
    public async Task<ServiceResult<WorkLocationDto>> CreateAsync(CreateWorkLocationDto dto, CancellationToken ct = default)
    {
        var error = await ValidateAsync(dto, ct);
        if (error is not null) return ServiceResult<WorkLocationDto>.Fail(error);

        if (await repo.CountAsync(new WorkLocationByNameSpecification(dto.Name), ct) > 0)
            return ServiceResult<WorkLocationDto>.Fail($"A work location named '{dto.Name}' already exists.", ServiceErrorType.Conflict);

        var location = new WorkLocation();
        Apply(location, dto);

        repo.Add(location);
        await unit.Complete();

        return ServiceResult<WorkLocationDto>.Success(await LoadDtoAsync(location.Id, ct));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var location = await repo.GetByIdAsync(id, ct);

        if (location is null) return ServiceResult<bool>.Fail("Work location not found.", ServiceErrorType.NotFound);

        repo.Remove(location);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<WorkLocationDto>> GetAllAsync(string? search, int? type, CancellationToken ct = default)  =>
        (await repo.ListAsync(new WorkLocationSearchSpecification(search, (WorkLocationType?)type), ct)).Select(ToDto).ToList();

    public async Task<WorkLocationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var location = await repo.GetEntityWithSpec(new WorkLocationByIdWithBranchSpefication(id), ct);
        return location is null ? null : ToDto(location);
    }

    public async Task<ServiceResult<WorkLocationDto>> UpdateAsync(Guid id, UpdateWorkLocationDto dto, CancellationToken ct = default)
    {
        var location = await repo.GetByIdAsync(id, ct);
        if (location is null) return ServiceResult<WorkLocationDto>.Fail("Work location not found.", ServiceErrorType.NotFound);

        var error = await ValidateAsync(dto, ct);
        if (error is not null) return ServiceResult<WorkLocationDto>.Fail(error);

        if (await repo.CountAsync(new WorkLocationByNameSpecification(dto.Name, excludeId: id), ct) > 0)
            return ServiceResult<WorkLocationDto>.Fail($"A work location named '{dto.Name}' already exists.", ServiceErrorType.Conflict);

        Apply(location, dto);
        location.IsActive = dto.IsActive;
        await unit.Complete();

        return ServiceResult<WorkLocationDto>.Success(await LoadDtoAsync(id, ct));
    }

    private async Task<string?> ValidateAsync(CreateWorkLocationDto dto, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(WorkLocationType), dto.Type)) return "Invalid work location type.";
        var type = (WorkLocationType)dto.Type;

        if (type != WorkLocationType.Remote && (dto.Latitude is null || dto.Longitude is null || dto.RadiusMeters is null))
            return "Latitude, longitude and radius are required for this location type.";
        
        if (type == WorkLocationType.Temporary)
        {
            if (dto.ValidFrom is null || dto.ValidTo is null) return "Temporary locations need a start and end date.";
            if (dto.ValidTo < dto.ValidFrom) return "End date must be on or after the start date.";
        }

        if (dto.BranchId is Guid bid && await branches.GetByIdAsync(bid, ct) is null)
            return "Selected branch was not found.";

        return null;
    }

    private static void Apply(WorkLocation l, CreateWorkLocationDto dto)
    {
        var type = (WorkLocationType)dto.Type;
        var remote = type == WorkLocationType.Remote;
        var temporary = type == WorkLocationType.Temporary;

        l.Name = dto.Name;
        l.Type = type;
        l.BranchId = dto.BranchId;
        l.Latitude = remote ? null : dto.Latitude;
        l.Longitude = remote ? null : dto.Longitude;
        l.RadiusMeters = remote ? null : dto.RadiusMeters;
        l.ValidFrom = temporary ? dto.ValidFrom : null;
        l.ValidTo = temporary ? dto.ValidTo : null;
    }

    private async Task<WorkLocationDto> LoadDtoAsync(Guid id, CancellationToken ct) =>
        ToDto((await repo.GetEntityWithSpec(new WorkLocationByIdWithBranchSpefication(id), ct))!);

    internal static WorkLocationDto ToDto(WorkLocation l) => new(
        l.Id,
        l.Name,
        (int)l.Type,
        l.Latitude,
        l.Longitude,
        l.RadiusMeters,
        l.BranchId,
        l.Branch?.Name,
        l.ValidFrom,
        l.ValidTo,
        l.IsActive,
        l.CreatedAtUtc
    );

}