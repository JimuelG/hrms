using Core.Common;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Branches;

public class BranchService(
    IUnitOfWork unit, 
    IGenericRepository<Branch> repo,
    IFeatureGate featureGate)
    : IBranchService
{
    public async Task<ServiceResult<BranchDto>> CreateAsync(CreateBranchDto dto, CancellationToken ct = default)
    {
        var currentCount = await repo.CountAsync(new BranchSearchSpecification(null), ct);
        await featureGate.EnsureWithinLimitAsync(UsageType.Branches, currentCount, ct);
        
        var duplicate = await repo.CountAsync(new BranchByCodeSpecification(dto.Code), ct);
        if (duplicate > 0)
            return ServiceResult<BranchDto>.Fail($"A branch with code '{dto.Code}' already exists.", ServiceErrorType.Conflict);

        var branch = new Branch
        {
            Name = dto.Name,
            Code = dto.Code,
            AddressLine = dto.AddressLine,
            City = dto.City,
            Country = dto.Country,
            TimeZoneId = dto.TimeZoneId
        };

        repo.Add(branch);
        await unit.Complete();
        return ServiceResult<BranchDto>.Success(ToDto(branch));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await repo.GetByIdAsync(id, ct);
        if (branch is null) return ServiceResult<bool>.Fail("Branch not found.", ServiceErrorType.NotFound);

        repo.Remove(branch);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<BranchDto>> GetAllAsync(string? search, CancellationToken ct = default) =>
        (await repo.ListAsync(new BranchSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<BranchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await repo.GetByIdAsync(id, ct);
        return branch is null ? null : ToDto(branch);
    }

    public async Task<ServiceResult<BranchDto>> UpdateAsync(Guid id, UpdateBranchDto dto, CancellationToken ct = default)
    {
        var branch = await repo.GetByIdAsync(id, ct);
        if (branch is null) return ServiceResult<BranchDto>.Fail("Branch not found.", ServiceErrorType.NotFound);

        var duplicate = await repo.CountAsync(new BranchByCodeSpecification(dto.Code, excludeId: id), ct);

        if (duplicate > 0)
            return ServiceResult<BranchDto>.Fail($"A branch with code '{dto.Code}' already exists.", ServiceErrorType.Conflict);

        branch.Name = dto.Name;
        branch.Code = dto.Code;
        branch.AddressLine = dto.AddressLine;
        branch.City = dto.City;
        branch.Country = dto.Country;
        branch.TimeZoneId = dto.TimeZoneId;
        branch.IsActive = dto.IsActive;

        await unit.Complete();
        return ServiceResult<BranchDto>.Success(ToDto(branch));
    }

    private static BranchDto ToDto(Branch b) =>
        new(
            b.Id,
            b.Name,
            b.Code,
            b.AddressLine,
            b.City,
            b.Country,
            b.TimeZoneId,
            b.IsActive,
            b.CreatedAtUtc
        );
}