using System.Runtime.CompilerServices;
using Core.Common;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Departments;

public sealed class DepartmentService(
    IUnitOfWork unit, 
    IGenericRepository<Department> repo,
    IFeatureGate featureGate)
    : IDepartmentService
{
    public async Task<ServiceResult<DepartmentDto>> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default)
    {
        var currentCount = await repo.CountAsync(new DepartmentSearchSpecification(dto.Code), ct);
        await featureGate.EnsureWithinLimitAsync(UsageType.Departments, currentCount, ct);

        var duplicate = await repo.CountAsync(new DepartmentSearchSpecification(dto.Code), ct);
        if (duplicate > 0)
            return ServiceResult<DepartmentDto>.Fail($"A branch with code '{dto.Code}' already exists." , ServiceErrorType.Conflict);

        var department = new Department
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description
        };

        repo.Add(department);
        await unit.Complete();
        return ServiceResult<DepartmentDto>.Success(ToDto(department));

    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var department = await repo.GetByIdAsync(id, ct);
        if (department is null) 
        return ServiceResult<bool>.Fail("Department not found.", ServiceErrorType.NotFound);
        
        repo.Remove(department);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(string? search, CancellationToken ct = default) =>
        (await repo.ListAsync(new DepartmentSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var department = await repo.GetByIdAsync(id, ct);
        return department is null ? null : ToDto(department);
    }

    public async Task<ServiceResult<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentDto dto, CancellationToken ct = default)
    {
        var department = await repo.GetByIdAsync(id, ct);
        if (department is null)
            return ServiceResult<DepartmentDto>.Fail("Department not found.", ServiceErrorType.NotFound);
        
        var duplicate = await repo.CountAsync(new DepartmentByCodeSpecification(dto.Code, excludeId: id), ct);
        if (duplicate > 0)
            return ServiceResult<DepartmentDto>.Fail($"A department with code '{dto.Code}' already exists.", ServiceErrorType.Conflict);

        department.Name = dto.Name;
        department.Code = dto.Code;
        department.Description = dto.Description;
        department.IsActive = dto.IsActive;

        await unit.Complete();
        return ServiceResult<DepartmentDto>.Success(ToDto(department));
    }

    private static DepartmentDto ToDto(Department d) =>
     new(
        d.Id,
        d.Name,
        d.Code,
        d.Description,
        d.IsActive,
        d.CreatedAtUtc
     );
}