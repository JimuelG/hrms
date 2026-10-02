import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

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
export class ShellComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  theme = inject(ThemeService);

  sidebarOpen = signal(false);

  navItems = [
    { label: 'Dashboard', path: '/dashboard', icon: 'fa-gauge' },
    { label: 'Branches', path: '/organization/branches', icon: 'fa-building' },
    { label: 'Departments', path: '/organization/departments', icon: 'fa-sitemap'}
  ];

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
