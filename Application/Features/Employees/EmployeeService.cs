using Core.Common;
using Core.DTOs.Employees;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public sealed class EmployeeService(
    IUnitOfWork unit,
    IGenericRepository<Employee> employees,
    IGenericRepository<Branch> branches,
    IGenericRepository<Department> departments,
    IGenericRepository<Position> positions,
    IFeatureGate featureGate,
    ITimelineService timeline) : IEmployeeService
{
    private static readonly EmployeeStatus[] AllowedInitialStatuses = [EmployeeStatus.Probationary, EmployeeStatus.Regular];

    public async Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeDto dto, CancellationToken ct = default)
    {
        if (!AllowedInitialStatuses.Contains((EmployeeStatus)dto.Status))
            return ServiceResult<EmployeeDto>.Fail(
                "New employees can only start as Probitionary or Regular. Other statuses are set through dedicated workflows.");

        var currentCount = await employees.CountAsync(new EmployeeSearchSpecification(null), ct);
        await featureGate.EnsureWithinLimitAsync(UsageType.Employees, currentCount, ct);

        if (await employees.CountAsync(new EmployeeByNumberSpecification(dto.EmployeeNumber), ct) > 0)
            return ServiceResult<EmployeeDto>.Fail($"Employee number '{dto.EmployeeNumber}' is already in use.", ServiceErrorType.Conflict);
        
        if (await employees.CountAsync(new EmployeeByEmailSpecification(dto.Email), ct) > 0)
            return ServiceResult<EmployeeDto>.Fail($"Email '{dto.Email}' is already in use.", ServiceErrorType.Conflict);

        var fkError = await ValidateFofeignKeysAsync(dto.BranchId, dto.DepartmentId, dto.PositionId, dto.ManagerId, selfId: null, ct);
        if (fkError is not null) return ServiceResult<EmployeeDto>.Fail(fkError);

        var employee = new Employee
        {
          EmployeeNumber = dto.EmployeeNumber,
          FirstName = dto.FirstName,
          LastName = dto.LastName,
          Email = dto.Email,
          Phone = dto.Phone,
          DateOfBirth = dto.DateOfBirth,
          BranchId = dto.BranchId,
          DepartmentId = dto.DepartmentId,
          PositionId = dto.PositionId,
          ManagerId = dto.ManagerId,
          EmploymentType = (EmploymentType)dto.EmploymentType,
          HireDate = dto.HireDate,
          Status = (EmployeeStatus)dto.Status  
        };

        employees.Add(employee);
        timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.Hired, $"Hired as {((EmploymentType)dto.EmploymentType).ToDisplayName()}", $"Joined on {dto.HireDate:yyyy-MM-dd}.");
        await unit.Complete();

        var saved = await employees.GetEntityWithSpec(new EmployeeWithRelationsByIdSpecification(employee.Id), ct);
        return ServiceResult<EmployeeDto>.Success(ToDto(saved!));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct);
        if (employee is null) 
            return ServiceResult<bool>.Fail("Employee not found", ServiceErrorType.NotFound);

        var directReportCount = await employees.CountAsync(new EmployeesByManagerSpecification(id), ct);
        if (directReportCount > 0)
            return ServiceResult<bool>.Fail(
                $"This employee manages {directReportCount} other employee(s). Reassign their reports first.");


        employees.Remove(employee);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(string? search, CancellationToken ct = default) => 
        (await employees.ListAsync(new EmployeeSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeWithRelationsByIdSpecification(id), ct);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(id, ct);
        if (employee is null) return ServiceResult<EmployeeDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        if (dto.ManagerId == id)
            return ServiceResult<EmployeeDto>.Fail("An employee cannot be their own manager.");

        var fkError = await ValidateFofeignKeysAsync(dto.BranchId, dto.DepartmentId, dto.PositionId, dto.ManagerId, selfId: id, ct);
        if (fkError is not null) return ServiceResult<EmployeeDto>.Fail(fkError);

        var oldStatus = employee.Status;
        var oldBranchId = employee.BranchId;
        var oldDepartmentId = employee.DepartmentId;
        var oldPositionId = employee.PositionId;
        var oldManagerId = employee.ManagerId;

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Email = dto.Email;
        employee.Phone = dto.Phone;
        employee.DateOfBirth = dto.DateOfBirth;
        employee.BranchId = dto.BranchId;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.ManagerId = dto.ManagerId;
        employee.EmploymentType = (EmploymentType)dto.EmploymentType;
        employee.HireDate = dto.HireDate;
        employee.Status = (EmployeeStatus)dto.Status;
        
        if (oldStatus != (EmployeeStatus)dto.Status)
            timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.Hired, $"Status changed to {((EmployeeStatus)dto.Status).ToDisplayName()}", $"Previously {oldStatus}");
        if (oldBranchId != dto.BranchId)
            timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.BranchChanged, "Branch changed");
        if (oldDepartmentId != dto.DepartmentId)
            timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.DepartmentChanged, "Department changed");
        if (oldPositionId != dto.PositionId)
            timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.PositionChanged, "Position changed");
        if (oldManagerId != dto.ManagerId)
            timeline.Record(TimelineSubjectType.Employee, employee.Id, TimeLineEventType.ManagerChanged, "Manager changed");

        await unit.Complete();
        var saved = await employees.GetEntityWithSpec(new EmployeeWithRelationsByIdSpecification(id), ct);
        return ServiceResult<EmployeeDto>.Success(ToDto(saved!));
    }

    public async Task<IReadOnlyList<EmployeeSummaryDto>> GetEligibleManagersAsync(Guid? excludeEmployeeId, CancellationToken ct = default) =>
        (await employees.ListAsync(new EligibleManagersSpecification(excludeEmployeeId), ct))
            .Select(e => new EmployeeSummaryDto(e.Id, $"{e.FirstName} {e.LastName}", e.Position.Title))
            .ToList();

    private async Task<string?> ValidateFofeignKeysAsync(
        Guid branchId,
        Guid departmentId,
        Guid positionId,
        Guid? managerId,
        Guid? selfId,
        CancellationToken ct)
    {
        if (await branches.GetByIdAsync(branchId, ct) is null) return "Selected branch was not found.";
        if (await departments.GetByIdAsync(departmentId, ct) is null) return "Selected department was not found";
        if (await positions.GetByIdAsync(positionId, ct) is null) return "Selected position was not found";

        if (managerId is Guid mid)
        {
            if (mid == selfId) return "An employee cannont be their own manager.";
            if (await employees.GetByIdAsync(mid, ct) is null) return "Selected manager was not found";
        }

        return null;
    }

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id,
        e.EmployeeNumber,
        e.FirstName,
        e.LastName,
        e.Email,
        e.Phone,
        e.DateOfBirth,
        e.BranchId,
        e.Branch.Name,
        e.DepartmentId,
        e.Department.Name,
        e.PositionId,
        e.Position.Title,
        e.ManagerId,
        e.Manager is null ? null : $"{e.Manager.FirstName} {e.Manager.LastName}",
        (int)e.EmploymentType,
        e.HireDate,
        (int)e.Status,
        e.CreatedAtUtc);
}