import { Routes } from '@angular/router';
import { LoginComponent } from './features/account/login/login.component';
import { authGuard } from './core/guards/auth-guard';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ShellComponent } from './layout/shel/shell/shell.component';
import { BranchesComponent } from './features/organization/branches/branches/branches.component';
import { DepartmentsComponent } from './features/organization/deparments/departments/departments.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent},
    {
        path: '',
        canActivate: [authGuard],
        component: ShellComponent,
        children: [
            { path: 'dashboard', component: DashboardComponent },
            { path: 'organization/branches', component: BranchesComponent},
            { path: 'organization/departments', component: DepartmentsComponent}
        ]
    },
    { path: '', redirectTo: 'login', pathMatch: 'full'}
];
