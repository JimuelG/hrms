import { Routes } from '@angular/router';
import { LoginComponent } from './features/account/login/login.component';
import { authGuard } from './core/guards/auth-guard';
import { DashboardComponent } from './features/dashboard/dashboard.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent},
    {
        path: 'dashboard',
        canActivate: [authGuard],
        component: DashboardComponent
    },
    { path: '', redirectTo: 'login', pathMatch: 'full'}
];
