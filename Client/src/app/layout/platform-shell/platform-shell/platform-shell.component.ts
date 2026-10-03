import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-platform-shell',
  imports: [
    RouterLink,
    RouterOutlet,
    RouterLinkActive
  ],
  templateUrl: './platform-shell.component.html',
  styleUrl: './platform-shell.component.scss',
})
export class PlatformShellComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  theme = inject(ThemeService);

  navItems = [
    { label: 'Tenants', path: '/platform/tenants', icon: 'fa-building-column' }
  ];

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
