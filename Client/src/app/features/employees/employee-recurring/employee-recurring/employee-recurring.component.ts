import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { ADJUSTMENT_TAX_LABELS, AdjustmentTax, RecurringItem } from '../../../../shared/models/payroll';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Kind = 'allowance' | 'deduction' | 'loan';

@Component({
  selector: 'app-employee-recurring',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './employee-recurring.component.html',
  styleUrl: './employee-recurring.component.scss',
})
export class EmployeeRecurringComponent {
  private payroll = inject(PayrollService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly peso = peso;
  readonly dateLabel = dateLabel;
  readonly taxLabel = ADJUSTMENT_TAX_LABELS;
  readonly taxOptions = [AdjustmentTax.NonTaxable, AdjustmentTax.Taxable];

  employeeId = input.required<string>();

  items = signal<RecurringItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  formOpen = signal(false);
  formError = signal<string | null>(null);
  saving = signal(false);
  form = {
    kind: 'allowance' as Kind,
    label: '',
    amount: 0,
    tax: AdjustmentTax.NonTaxable,
    from: '',
    to: '',
    totalOwed: 0
  };

  constructor() {
    effect(() => { this.employeeId(); void this.load(); });
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.items.set(await firstValueFrom(this.payroll.listRecurring(this.employeeId())));
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  openForm(): void {
    this.form = { kind: 'allowance', label: '', amount: 0, tax: AdjustmentTax.NonTaxable, from: '', to: '', totalOwed: 0 };
    this.formError.set(null);
    this.formOpen.set(true);
  }

  async save(): Promise<void> {
    const f = this.form;
    if (!f.label.trim()) { this.formError.set('Give the item a label.'); return; }
    if (!f.amount || f.amount <= 0) { this.formError.set('Enter the amount as a positive number.'); return; } 
    if (!f.from) { this.formError.set('Choose when it starts.'); return; }
    if (f.kind === 'loan' && (!f.totalOwed || f.totalOwed <= 0)) { this.formError.set('Enter the total owed.'); return; }

    this.saving.set(true);
    this.formError.set(null);
    try {
      await firstValueFrom(this.payroll.createRecurring({
        employeeId: this.employeeId(),
        label: f.label.trim(),
        amount: f.kind === 'allowance' ? f.amount : -f.amount,
        tax: f.tax,
        from: f.from,
        to: f.to || null,
        totalOwed: f.kind === 'loan' ? f.totalOwed : null
      }));
      this.formOpen.set(false);
      await this.load();
    } catch (err) {
      this.formError.set(errorMessage(err));
    } finally {
      this.saving.set(false);
    }
  }

  async deactivate(i: RecurringItem): Promise<void> {
    if (!confirm(`Stop "${i.label}"? Future runs won't include it, Past repayments are kept.`)) return;
    try {
      await firstValueFrom(this.payroll.deactivateRecurring(i.id));
      await this.load();
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }
}
