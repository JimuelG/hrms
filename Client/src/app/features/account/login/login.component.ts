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
  newPassword = '';
  confirmPassword = '';

  errorMessage = signal<string | null>(null);
  tenantChoices = signal<TenantChoice[] | null>(null);
  mustChange = signal(false);
  busy = signal(false);

  submit(): void {
    this.errorMessage.set(null);

    this.authService.login({ email: this.email, password: this.password, tenantSlug: this.tenantSlug || undefined}).subscribe({
      next: (res) => {
        this.busy.set(false);
        if (res.passwordChangeRequired) {
          this.mustChange.set(true);
          return;
        }
        if (res.tenantSelectionRequired) {
          this.tenantChoices.set(res.tenants)
          return;
        }
        this.goHome();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(this.messageFor(err));
      }
    })
  }

  changePassword(): void {
    this.errorMessage.set(null);
    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage.set('The two passwords don\'t match.');
      return;
    }

    this.busy.set(true);

    this.authService.changeTemporaryPassword({
      email: this.email,
      currentPassword: this.password,
      newPassword: this.newPassword,
      tenantSlug: this.tenantSlug || null
    }).subscribe({
      next: (res) => {
        this.busy.set(false);
        this.password = this.newPassword;
        this.newPassword = '';
        this.confirmPassword = '';
        this.mustChange.set(false);

        if (res.tenantSelectionRequired) {
          this.tenantChoices.set(res.tenants);
          return;
        }
        this.goHome();
      },
      error: (err) => {
        this.busy.set(false);
        this.errorMessage.set(this.messageFor(err));
      }
    });
  }

  selectTenant(slug: string): void {
    this.tenantSlug = slug;
    this.submit();
  }

  private goHome(): void {
    this.router.navigate([this.authService.isPlatformAdmin() ? 'platform/tenants' : '/dashboard']);
  }

  private messageFor(err: any): string {
    if (err?.status == 423) return 'Account temporarily locked. Try again later.';
    return err?.error?.title || 'Invalid email or password.';
  }
}

