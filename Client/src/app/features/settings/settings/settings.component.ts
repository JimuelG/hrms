import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TenantSettingsService } from '../../../core/services/tenant-settings.service';
import { UpdateTenantSettings, WORKING_DAY_OPTIONS, WorkingDay } from '../../../shared/models/tenant-settings';
import { extractApiError } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-settings',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit{
  private tenantSettingsService = inject(TenantSettingsService);

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  saved = signal(false);

  dayOptions = WORKING_DAY_OPTIONS;
  form = signal<UpdateTenantSettings>(this.emptyForm());

  ngOnInit(): void {
    this.tenantSettingsService.get().subscribe({
      next: (s) => {
        this.form.set({ ...s });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  updateField<K extends keyof UpdateTenantSettings>(key: K, value: UpdateTenantSettings[K]): void {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  isDaySelected(day: WorkingDay): boolean {
    return (this.form().workingDays & day) === day;
  }

  toggleDay(day: WorkingDay): void {
    this.form.update((f) => ({
      ...f,
      workingDays: (f.workingDays & day) === day 
        ? f.workingDays & ~day 
        : f.workingDays | day
    }));
  }

  save(): void {
    this.error.set(null);
    this.saved.set(false);
    this.saving.set(true);

    this.tenantSettingsService.update(this.form()).subscribe({
      next: (s) => {
        this.form.set({ ...s });
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(extractApiError(err));
      }
    });
  }

  private emptyForm(): UpdateTenantSettings {
    return {
      companyLegalName: '',
      logoUrl: '',
      primaryColor: '#1C4ED8',
      addressLine: '',
      city: '',
      country: '',
      contactEmail: '',
      contactPhone: '',
      timeZoneId: 'UTC',
      currency: 'PHP',
      dateFormat: 'yyyy-MM-dd',
      workingDays: 0
    }
  }
}
