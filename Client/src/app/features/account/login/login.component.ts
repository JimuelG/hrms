import { Component, inject, signal } from '@angular/core';
import { TenantChoice } from '../../../shared/models/auth';
import { AuthService } from '../../../core/auth/auth.service';
import { Router } from '@angular/router';
import { FormBuilder, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  tenantSlug = '';
  errorMessage = signal<string | null>(null);
  tenantChoices = signal<TenantChoice[] | null>(null);

  submit(): void {
    this.errorMessage.set(null);

    this.authService.login({ email: this.email, password: this.password, tenantSlug: this.tenantSlug || undefined}).subscribe({
      next: (res) => {
        if (res.tenantSelectionRequired) {
          this.tenantChoices.set(res.tenants)
          return;
        }
        this.router.navigate([this.authService.isPlatformAdmin() ? 'platform/tenants' : '/dashboard']);
      },
      error: (err) => {
        if (err.status === 423) {
          this.errorMessage.set('Account tempoarily locked. Try again later.');
        }
        else {
          this.errorMessage.set('Invalid email or password.');
        }
      }
    })
  }

  selectTenant(slug: string): void {
    this.tenantSlug = slug;
    this.submit();
  }
}

