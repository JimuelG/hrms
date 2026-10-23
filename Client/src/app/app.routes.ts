import { Routes } from '@angular/router';
import { LoginComponent } from './features/account/login/login.component';
import { authGuard } from './core/guards/auth-guard';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ShellComponent } from './layout/shell/shell/shell.component';
import { BranchesComponent } from './features/organization/branches/branches/branches.component';
import { DepartmentsComponent } from './features/organization/deparments/departments/departments.component';
import { SettingsComponent } from './features/settings/settings/settings.component';
import { platformAdminGuard } from './core/guards/platform-admin-guard';
import { PlatformShellComponent } from './layout/platform-shell/platform-shell/platform-shell.component';
import { TenantsComponent } from './features/platform/tenants/tenants/tenants.component';
import { PositionsComponent } from './features/organization/positions/positions/positions.component';
import { EmployeeListComponent } from './features/employees/employee-list/employee-list/employee-list.component';
import { EmployeeDetailComponent } from './features/employees/employee-detail/employee-detail/employee-detail.component';
import { OrgChartComponent } from './features/employees/org-chart/org-chart/org-chart.component';
import { JobPostingsComponent } from './features/recruitment/job-postings/job-postings/job-postings.component';
import { ApplicantsComponent } from './features/recruitment/applicants/applicants/applicants.component';
import { PipelineComponent } from './features/recruitment/pipeline/pipeline/pipeline.component';
import { ApplicationDetailComponent } from './features/recruitment/application-detail/application-detail/application-detail.component';
import { OnboardingTemplatesComponent } from './features/recruitment/onboarding-templates/onboarding-templates/onboarding-templates.component';
import { MyAttendanceComponent } from './features/attendance/my-attendance/my-attendance/my-attendance.component';
import { permissionGuard } from './core/guards/permission-guard';
import { PERMISSIONS } from './shared/constants/permissions';
import { AttendanceRecordsComponent } from './features/attendance/records/attendance-records/attendance-records.component';
import { WorkSchedulesComponent } from './features/attendance/work-schedules/work-schedules/work-schedules.component';
import { WorkLocationsComponent } from './features/attendance/work-locations/work-locations/work-locations.component';
import { ApprovalsComponent } from './features/attendance/approvals/approvals/approvals.component';
import { MyLeaveComponent } from './features/leave/my-leave/my-leave/my-leave.component';
import { LeaveApprovalsComponent } from './features/leave/approvals/leave-approvals/leave-approvals.component';
import { HrRequestsComponent } from './features/leave/hr-requests/hr-requests/hr-requests.component';
import { LeaveTypesComponent } from './features/leave/leave-types/leave-types/leave-types.component';
import { HolidaysComponent } from './features/leave/holidays/holidays/holidays.component';
import { PayrollRunsComponent } from './features/payroll/runs/payroll-runs/payroll-runs.component';
import { PayrollRunDetailComponent } from './features/payroll/run-detail/payroll-run-detail/payroll-run-detail.component';
import { PayrollSettingsComponent } from './features/payroll/settings/payroll-settings/payroll-settings.component';
import { StatutoryRulesComponent } from './features/payroll/rules/statutory-rules/statutory-rules.component';
import { MyPayslipsComponent } from './features/payroll/my-payslips/my-payslips/my-payslips.component';
import { RunPayslipComponent } from './features/payroll/run-payslip/run-payslip/run-payslip.component';
import { WorkforceReportComponent } from './features/reports/workforce/workforce-report/workforce-report.component';
import { AttendanceReportComponent } from './features/reports/attendance/attendance-report/attendance-report.component';
import { LeaveReportComponent } from './features/reports/leave/leave-report/leave-report.component';
import { RecruitmentReportComponent } from './features/reports/recruitment/recruitment-report/recruitment-report.component';
import { AlertsComponent } from './features/alerts/alerts/alerts.component';
import { AuditLogComponent } from './features/audit/audit-log/audit-log.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent},
    {
        path: '',
        canActivate: [authGuard],
        component: ShellComponent,
        children: [
            { path: '', component: DashboardComponent },
            { path: 'dashboard', component: DashboardComponent },
            { path: 'organization/branches', component: BranchesComponent},
            { path: 'organization/departments', component: DepartmentsComponent},
            { path: 'settings', component: SettingsComponent},
            { path: 'organization/positions', component: PositionsComponent},
            { path: 'employees', component: EmployeeListComponent},
            { path: 'employees/org-chart', component: OrgChartComponent},
            { path: 'employees/:id', component: EmployeeDetailComponent},
            { path: 'recruitment/applicants', component: ApplicantsComponent},
            { path: 'recruitment/job-postings', component: JobPostingsComponent},
            { path: 'recruitment/pipeline/:jobPostingId', component: PipelineComponent},
            { path: 'recruitment/applications/:id', component: ApplicationDetailComponent},
            { path: 'recruitment/onboarding-templates', component: OnboardingTemplatesComponent},
            { path: 'attendance', component: MyAttendanceComponent, canActivate: [permissionGuard(PERMISSIONS.attendance.clock)]},
            { path: 'attendance/approvals', component: ApprovalsComponent, canActivate: [permissionGuard(PERMISSIONS.attendance.approve)]},
            { path: 'attendance/records', component: AttendanceRecordsComponent, canActivate: [permissionGuard(PERMISSIONS.attendance.read)]},
            { path: 'attendance/schedules', component: WorkSchedulesComponent, canActivate: [permissionGuard(PERMISSIONS.workSchedules.read)]},
            { path: 'attendance/locations', component: WorkLocationsComponent, canActivate: [permissionGuard(PERMISSIONS.workLocations.read)]},
            { path: 'leave', component: MyLeaveComponent, canActivate: [permissionGuard(PERMISSIONS.leave.request)]},
            { path: 'leave/approvals', component: LeaveApprovalsComponent, canActivate: [permissionGuard(PERMISSIONS.leave.request)]},
            { path: 'leave/requests', component: HrRequestsComponent, canActivate: [permissionGuard(PERMISSIONS.leave.manage)]},
            { path: 'leave/types', component: LeaveTypesComponent, canActivate: [permissionGuard(PERMISSIONS.leave.manage)]},
            { path: 'leave/holidays', component: HolidaysComponent, canActivate: [permissionGuard(PERMISSIONS.leave.manage)]},
            { path: 'payroll/runs', component: PayrollRunsComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.read)]},
            { path: 'payroll/runs/:id', component: PayrollRunDetailComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.read)]},
            { path: 'payroll/rules', component: StatutoryRulesComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.readRules)]},
            { path: 'payroll/settings', component: PayrollSettingsComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.manageSettings)]},
            { path: 'payslips', component: MyPayslipsComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.viewOwn)]},
            { path: 'payslips/runs/:runId/payslip/:employeeId', component: RunPayslipComponent, canActivate: [permissionGuard(PERMISSIONS.payroll.read)]},
            { path: 'reports/workforce', component: WorkforceReportComponent, canActivate: [permissionGuard(PERMISSIONS.reports.view)]},
            { path: 'reports/attendance', component: AttendanceReportComponent, canActivate: [permissionGuard(PERMISSIONS.reports.view)]},
            { path: 'reports/leave', component: LeaveReportComponent, canActivate: [permissionGuard(PERMISSIONS.reports.view)]},
            { path: 'reports/recruitment', component: RecruitmentReportComponent, canActivate: [permissionGuard(PERMISSIONS.reports.view)]},
            { path: 'alerts', component: AlertsComponent, canActivate: [permissionGuard(PERMISSIONS.alerts.view)]},
            { path: 'audit', component: AuditLogComponent, canActivate: [permissionGuard(PERMISSIONS.audit.view)]},
        ]
    },
    {
        path: 'platform',
        canActivate: [platformAdminGuard],
        component: PlatformShellComponent,
        children: [
            { path: 'tenants', component: TenantsComponent },
            { path: '', redirectTo: 'tenants', pathMatch: 'full'},
        ]
    },
    { path: '', redirectTo: 'login', pathMatch: 'full'}
];
