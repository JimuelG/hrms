import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PlatformTenantService } from '../../../../core/services/platform-tenant.service';
import { CreateTenantValue, PlatformTenant, TenantStatus } from '../../../../shared/models/platforrm-tenant';
import { extractApiError } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-tenants',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss',
})
export class TenantsComponent implements OnInit{
  private platformTenantService = inject(PlatformTenantService);
  TenantStatus = TenantStatus;

  tenants = signal<PlatformTenant[]>([]);
  loading = signal(true);

  drawerOpen = signal(false);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: CreateTenantValue = { name: '', slug: ''};

  ngOnInit(): void {
    this.load();  
  }

  load(): void {
    this.loading.set(true);
    this.platformTenantService.getAll().subscribe({
      next: (data) => {
        this.tenants.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.form = { name: '', slug: '' };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);
    this.platformTenantService.create(this.form).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err));
      }
    });
  }

  toggleStatus(tenant: PlatformTenant): void {
    const action = tenant.status === TenantStatus.Active
      ? this.platformTenantService.suspend(tenant.id)
      : this.platformTenantService.reactivate(tenant.id);
    action.subscribe(() => this.load());
  }
}
