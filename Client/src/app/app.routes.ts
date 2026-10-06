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
import { JobPostingsComponent } from './features/recuitment/job-postings/job-postings/job-postings.component';
import { ApplicantsComponent } from './features/recuitment/applicants/applicants/applicants.component';
import { PipelineComponent } from './features/recuitment/pipeline/pipeline/pipeline.component';
import { ApplicationDetailComponent } from './features/recuitment/application-detail/application-detail/application-detail.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent},
    {
        path: '',
        canActivate: [authGuard],
        component: ShellComponent,
        children: [
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
