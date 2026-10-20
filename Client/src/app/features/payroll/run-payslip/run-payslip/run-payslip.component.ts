import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PayslipViewComponent } from '../../payslip/payslip-view/payslip-view.component';
import { PayrollService } from '../../../../core/services/payroll.service';
import { Payslip } from '../../../../shared/models/payroll';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-run-payslip',
  imports: [
    CommonModule,
    RouterLink,
    PayslipViewComponent
  ],
  templateUrl: './run-payslip.component.html',
  styleUrl: './run-payslip.component.scss',
})
export class RunPayslipComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private payroll = inject(PayrollService);

  readonly runId = this.route.snapshot.paramMap.get('runId')!;
  private employeeId = this.route.snapshot.paramMap.get('employeeId')!;
  payslip = signal<Payslip | null>(null);
  error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.payslip.set(await firstValueFrom(this.payroll.getPayslip(this.runId, this.employeeId)));
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }
}
