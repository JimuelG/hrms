using Application.Features.Approvals;
using Application.Features.Attendance;
using Application.Features.Branches;
using Application.Features.Dashboard;
using Application.Features.Departments;
using Application.Features.Employees;
using Application.Features.Leave;
using Application.Features.Onboarding;
using Application.Features.Payroll;
using Application.Features.Positions;
using Application.Features.Recruitment;
using Application.Features.Reports;
using Core.DTOs.Attendance;
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
        services.AddScoped<ITimeZoneResolver, EmployeeTimeZoneResolver>();
        services.AddScoped<IAttendanceCorrectionService, AttendanceCorrectionService>();
        services.AddScoped<IOvertimeService, OvertimeService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<ILeaveCalculationService, LeaveCalculationService>();
        services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        services.AddScoped<IApprovalQueryService, ApprovalQueryService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IApprovalSubjectHandler, LeaveApprovalHandler>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<ILeaveAttachmentService, LeaveAttachmentService>();
        services.AddScoped<ICompensationService, CompensationService>();
        services.AddScoped<IStatutoryService, StatutoryService>();
        services.AddScoped<IPayrollRunService, PayrollRunService>();
        services.AddScoped<IPayslipService, PayslipService>();
        services.AddScoped<IRecurringPayrollService, RecurringPayrollService>();
        services.AddScoped<IIdentifierService, IdentifierService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IWorkforceReportService, WorkforceReportService>();
        services.AddScoped<IAttendanceReportService, AttendanceReportService>();
        services.AddScoped<ILeaveReportService, LeaveReportService>();

        return services;
    }
}