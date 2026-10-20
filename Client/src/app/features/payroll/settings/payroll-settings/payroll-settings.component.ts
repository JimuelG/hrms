import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { ContributionDeduction, FREQUENCY_LABELS, PARTIAL_LABELS, PartialMonthContribution, PayFrequency, PayrollSettings, POLICY_LABELS } from '../../../../shared/models/payroll';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-payroll-settings',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './payroll-settings.component.html',
  styleUrl: './payroll-settings.component.scss',
})
export class PayrollSettingsComponent implements OnInit {
  private payroll = inject(PayrollService);

  readonly frequencies = [PayFrequency.SemiMonthly, PayFrequency.Monthly];
  readonly policies = [ContributionDeduction.SplitEvenly, ContributionDeduction.SecondPeriodOnly];
  readonly frequencyLabel = FREQUENCY_LABELS;
  readonly policyLabel = POLICY_LABELS;
  readonly partials = [PartialMonthContribution.FullPeriod, PartialMonthContribution.ByDaysEmployed, PartialMonthContribution.SkipIfPartial];
  readonly partialLabel = PARTIAL_LABELS;

  form = signal<PayrollSettings | null>(null);
  loadError = signal<string | null>(null);
  saveError = signal<string | null>(null);
  saved = signal(false);
  saving = signal(false);

  ngOnInit(): void {
    this.payroll.getSettings().subscribe({
      next: (s) => {
        this.form.set({ ...s });
      },
      error: (err) => {
        this.loadError.set(errorMessage(err))
      }
    })
  }

  patch(change: Partial<PayrollSettings>): void {
    this.form.update((f) => (f ? { ...f, ...change } : f));
    this.saved.set(false);
  }

  save(): void {
    const f = this.form();
    if (!f) return;
    this.saving.set(true);
    this.saveError.set(null);
    this.payroll.updateSettings(f).subscribe({
      next: (s) => {
        this.form.set({ ...s });
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err) => {
        this.saving.set(false);
        this.saveError.set(errorMessage(err));
      }
    })
  }
}
