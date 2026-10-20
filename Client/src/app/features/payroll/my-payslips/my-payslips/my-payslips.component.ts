import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { PayslipViewComponent } from '../../payslip/payslip-view/payslip-view.component';
import { PayrollService } from '../../../../core/services/payroll.service';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { Payslip, PayslipSummary } from '../../../../shared/models/payroll';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-my-payslips',
  imports: [
    CommonModule,
    PayslipViewComponent
  ],
  templateUrl: './my-payslips.component.html',
  styleUrl: './my-payslips.component.scss',
})
export class MyPayslipsComponent implements OnInit {
  private payroll = inject(PayrollService);
  readonly peso = peso;
  readonly dateLabel = dateLabel;

  list = signal<PayslipSummary[]>([]);
  selectedId = signal<string | null>(null);
  payslip = signal<Payslip | null>(null);
  loading = signal(true);
  loadError = signal<string | null>(null);
  detailError = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const rows = await firstValueFrom(this.payroll.listMyPayslip());
      this.list.set(rows);
      this.loadError.set(null);
      if (rows.length > 0) await this.select(rows[0]);
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async select(s: PayslipSummary): Promise<void> {
    this.selectedId.set(s.runId);
    this.payslip.set(null);
    this.detailError.set(null);
    try {
      this.payslip.set(await firstValueFrom(this.payroll.getMyPayslip(s.runId)));
    } catch (err) {
      this.detailError.set(errorMessage(err));
    }
  }
}
