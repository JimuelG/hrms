import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { Compensation } from '../../../../shared/models/payroll';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-employee-compensation',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './employee-compensation.component.html',
  styleUrl: './employee-compensation.component.scss',
})
export class EmployeeCompensationComponent {
  private payroll = inject(PayrollService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly peso = peso;
  readonly dateLabel = dateLabel;

  employeeId = input.required<string>();

  history = signal<Compensation[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  formOpen = signal(false);
  form = { monthlyBasicSalary: 0, effectiveFrom: '', reason: '' };
  formError = signal<string | null>(null);
  saving = signal(false);

  constructor() {
    effect(() => { this.employeeId(); void this.load(); });
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.history.set(await firstValueFrom(this.payroll.getCompensation(this.employeeId())));
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  openForm(): void {
    this.form = { monthlyBasicSalary: this.history()[0]?.monthlyBasicSalary ?? 0, effectiveFrom: '', reason: '' };
    this.formError.set(null);
    this.formOpen.set(true);
  }

  async save(): Promise<void> {
    const f = this.form;
    if (!f.monthlyBasicSalary || f.monthlyBasicSalary <= 0) { this.formError.set('Enter he monthly salary.'); return; }
    if (!f.effectiveFrom) { this.formError.set('Choose the date it takes effect.'); return; }
    if (!f.reason.trim()) { this.formError.set('Give a reason. It it the only record of why the salary changed.'); return; }

    this.saving.set(true);
    this.formError.set(null);
    try {
      await firstValueFrom(this.payroll.setCompensation(this.employeeId(), f.monthlyBasicSalary, f.effectiveFrom, f.reason.trim()));
      this.formOpen.set(false);
      await this.load();
    } catch (err) {
      this.formError.set(errorMessage(err));
    } finally {
      this.saving.set(false);
    }
  }
}
