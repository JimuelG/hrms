import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { PayrollLineKind, PayrollRunStatus, Payslip } from '../../../../shared/models/payroll';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';

@Component({
  selector: 'app-payslip-view',
  imports: [
    CommonModule
  ],
  templateUrl: './payslip-view.component.html',
  styleUrl: './payslip-view.component.scss',
})
export class PayslipViewComponent {
  payslip = input.required<Payslip>();

  readonly peso = peso;
  readonly dateLabel = dateLabel;


  earnings = computed(() => this.payslip().lines.filter((l) => l.kind === PayrollLineKind.Earning));
  deductions = computed(() => this.payslip().lines.filter((l) => l.kind === PayrollLineKind.Deduction));
  isDraft = computed(() => this.payslip().runStatus !== PayrollRunStatus.Finalized);

  print(): void {
    window.print();
  }
}
