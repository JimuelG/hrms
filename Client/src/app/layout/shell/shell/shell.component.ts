import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { PermissionService } from '../../../core/services/permission.service';
import { PERMISSIONS as P } from '../../../shared/constants/permissions';

interface NavItem { 
  label: string; 
  path: string; 
  icon: string; 
  permission?: string; 
  exact?: boolean}

interface NavSection { title?: string; items: NavItem[]; }

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit{
  private authService = inject(AuthService);
  private router = inject(Router);
  readonly permissions = inject(PermissionService);
  theme = inject(ThemeService);

  sidebarOpen = signal(false);

  private readonly sections: NavSection[] = [
    { items: [{ label: 'Dashboard', path: '/dashboard', icon: 'fa-gauge' }] },
    { title: 'People', items: [{ label: 'Employees', path: '/employees', icon: 'fa-users', permission: P.employees.read}] },
    { title: 'Organization', items: [
      { label: 'Branches', path: '/organization/branches', icon: 'fa-building', permission: P.branches.read },
      { label: 'Departments', path: '/organization/departments', icon: 'fa-sitemap', permission: P.departments.read },
      { label: 'Positions', path: '/organization/positions', icon: 'fa-id-badge', permission: P.positions.read}
    ]},
    { title: 'Recruitment', items: [
      { label: 'Job Postings', path: '/recruitment/job-postings', icon: 'fa-briefcase', permission: P.jobPostings.read },
      { label: 'Applicants', path: '/recruitment/applicants', icon: 'fa-user-group', permission: P.applicants.read },
      { label: 'Onboarding', path: '/recruitment/onboarding-templates', icon: 'fa-clipboard-check', permission: P.onboarding.read },
    ]},
    { title: 'Attendance', items: [
      { label: 'My Attendance', path: '/attendance', icon: 'fa-clock', permission: P.attendance.clock, exact: true },
      { label: 'Approvals', path: '/attendance/approvals', icon: 'fa-list-check', permission: P.attendance.approve },
      { label: 'Records', path: '/attendance/records', icon: 'fa-table-list', permission: P.attendance.read },
      { label: 'Work schedules', path: '/attendance/schedules', icon: 'fa-calendar-week', permission: P.workSchedules.read },
      { label: 'Work locations', path: '/attendance/locations', icon: 'fa-location-dot', permission: P.workLocations.read }
    ]},
    { title: 'Leave', items: [
      { label: 'My leave', path: '/leave', icon: 'fa-umbrella-beach', permission: P.leave.request, exact: true },
      { label: 'Approvals', path: '/leave/approvals', icon: 'fa-inbox', permission: P.leave.request },
      { label: 'Leave requests', path: '/leave/requests', icon: 'fa-list-check', permission: P.leave.manage },
      { label: 'Leave types', path: '/leave/types', icon: 'fa-tags', permission: P.leave.manage },
      { label: 'Holidays', path: '/leave/holidays', icon: 'fa-calendar-day', permission: P.leave.manage },
    ]},
    { title: 'Payroll', items: [
      { label: 'Payroll runs', path: '/payroll/runs', icon: 'fa-money-check-dollar', permission: P.payroll.read },
      { label: 'Statutory tables', path: '/payroll/rules', icon: 'fa-scale-balanced', permission: P.payroll.readRules },
      { label: 'Payroll settings', path: '/payroll/settings', icon: 'fa-sliders', permission: P.payroll.manageSettings },
      { label: 'My payslips', path: '/payslips', icon: 'fa-file-invoice-dollar', permission: P.payroll.viewOwn },
    ]},
    { title: 'Reports', items: [
      { label: 'Workforce', path: '/reports/workforce', icon: 'fa-chart-pie', permission: P.reports.view },
      { label: 'Attendance', path: '/reports/attendance', icon: 'fa-user-clock', permission: P.reports.view },
      { label: 'Leave', path: '/reports/leave', icon: 'fa-umbrella-beach', permission: P.reports.view },
      { label: 'Recruitment', path: '/reports/recruitment', icon: 'fa-user-plus', permission: P.reports.view },
    ]},
    { title: 'Alerts', items: [
      { label: 'Alerts', path: '/alerts', icon: 'fa-bell', permission: P.alerts.view }
    ]},
    { title: 'Audit', items: [
      { label: 'Audit trail', path: '/audit', icon: 'fa-clock-rotate-left', permission: P.audit.view }
    ]},
    { items: [{ label: 'Settings', path: '/settings', icon: 'fa-gear', permission: P.tenant.manageSettings }] }
  ]

  visibleSections = computed(() =>
    this.sections.map((s) => ({ ...s, items: s.items.filter((i) => !i.permission || this.permissions.can(i.permission)) })).filter((s) => s.items.length > 0));

  ngOnInit(): void {
    void this.permissions.load();
  }


  logout(): void {
    this.permissions.clear();
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
