using Core.Common;
using Core.DTOs.Organization;
using Core.Interfaces;
using Core.Entities;
using Core.Specifications;

namespace Application.Features.Positions;

public sealed class PositionService(
    IUnitOfWork unit,
    IGenericRepository<Position> repo) 
    : IPositionService
{
    public async Task<ServiceResult<PositionDto>> CreateAsync(CreatePositionDto dto, CancellationToken ct = default)
    {
        var duplicate = await repo.CountAsync(new PositionByCodeSpecification(dto.Code), ct);
        if (duplicate > 0)
            return ServiceResult<PositionDto>.Fail($"A position with code '{dto.Code}' already exists.", ServiceErrorType.Conflict);

        var position = new Position
        {
            Title = dto.Title,
            Code = dto.Code,
            Description = dto.Description
        };

        repo.Add(position);
        await unit.Complete();
        return ServiceResult<PositionDto>.Success(ToDto(position));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var position = await repo.GetByIdAsync(id, ct);
        if (position is null) return ServiceResult<bool>.Fail("Position not found.", ServiceErrorType.NotFound);

        repo.Remove(position);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<PositionDto>> GetAllAsync(string? search, CancellationToken ct = default) =>
        (await repo.ListAsync(new PositionSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<PositionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var position = await repo.GetByIdAsync(id, ct);
        return position is null ? null : ToDto(position);
    }

    public async Task<ServiceResult<PositionDto>> UpdateAsync(Guid id, UpdatePositionDto dto, CancellationToken ct = default)
    {
        var position = await repo.GetByIdAsync(id, ct);
        if (position is null) return ServiceResult<PositionDto>.Fail("Position not found", ServiceErrorType.NotFound);

        var duplicate = await repo.CountAsync(new PositionByCodeSpecification(dto.Code, excludeId: id), ct);

        if (duplicate > 0)
            return ServiceResult<PositionDto>.Fail($"A position with code '{dto.Code}' already exists.", ServiceErrorType.Conflict);

        position.Title = dto.Title;
        position.Code = dto.Code;
        position.Description = dto.Description;
        position.IsActive = dto.IsActive;

        await unit.Complete();
        return ServiceResult<PositionDto>.Success(ToDto(position));
    }

    private static PositionDto ToDto(Position p) =>
        new(
            p.Id,
            p.Title,
            p.Code,
            p.Description,
            p.IsActive,
            p.CreatedAtUtc
        );
}