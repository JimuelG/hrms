using Application.Features.Branches;
using Application.Features.Departments;
using Application.Features.Employees;
using Application.Features.Positions;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IEmergencyContactService, EmergencyContactService>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<ITimelineService, TimelineService>();

        return services;
    }
}