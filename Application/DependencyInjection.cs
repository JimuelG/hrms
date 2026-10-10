using Application.Features.Attendance;
using Application.Features.Branches;
using Application.Features.Departments;
using Application.Features.Employees;
using Application.Features.Onboarding;
using Application.Features.Positions;
using Application.Features.Recruitment;
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
        services.AddScoped<IJobPostingService, JobPostingService>();
        services.AddScoped<IApplicantService, ApplicantService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IJobOfferService, JobOfferService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IOnboardingTaskTemplateService, OnboardingTaskTemplateService>();
        services.AddScoped<IWorkScheduleService, WorkScheduleService>();
        services.AddScoped<IWorkLocationService, WorkLocationService>();
        services.AddScoped<IEmployeeWorkLocationService, EmployeeWorkLocationService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IEmployeeUserLinkService, EmployeeUserLinkService>();

        return services;
    }
}