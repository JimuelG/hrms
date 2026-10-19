import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { Router } from '@angular/router';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { PayFrequency, PayrollRunSummary, RUN_STATUS_LABELS, runStattusClass } from '../../../../shared/models/payroll';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-payroll-runs',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './payroll-runs.component.html',
  styleUrl: './payroll-runs.component.scss',
})
export class PayrollRunsComponent implements OnInit {
  private payroll = inject(PayrollService);
  private router = inject(Router);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;

  readonly statusLabel = RUN_STATUS_LABELS;
  readonly statusClass = runStattusClass;
  readonly peso = peso;
  readonly dateLabel = dateLabel;

  runs = signal<PayrollRunSummary[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);

  startOpen = signal(false);
  start = { periodStart: '', periodEnd: '', notes: '' };
  startError = signal<string | null>(null);
  starting = signal(false);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.runs.set(await firstValueFrom(this.payroll.listRuns()));
      this.loadError.set(null);
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  open(r: PayrollRunSummary): void {
    void this.router.navigate(['/payroll/runs', r.id]);
  }

  async openStart(): Promise<void> {
    this.startError.set(null);
    this.start = { periodStart: '', periodEnd: '', notes: '' };
    this.startOpen.set(true);

    try {
      const s = await firstValueFrom(this.payroll.getSettings());
      const t = new Date();
      const y = t.getFullYear(), m = t.getMonth(), d = t.getDate();
      const last = new Date(y, m + 1, 0).getDate();
      const iso = (day: number) => `${y}-${String(m + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;

      if (s.frequency === PayFrequency.Monthly) this.start = { periodStart: iso(1), periodEnd: iso(last), notes: '' };
      else if (d <= 15) this.start = { periodStart: iso(1), periodEnd: iso(15), notes: '' };
      else this.start = { periodStart: iso(16), periodEnd: iso(last), notes: '' };
    } catch {

    }
  }

  async submitStart(): Promise<void> {
    if (!this.start.periodStart || !this.start.periodEnd) { this.startError.set('Choose the period.'); return; }

    this.starting.set(true);
    this.startError.set(null);
    try {
      const detail = await firstValueFrom(this.payroll.startRun(this.start.periodStart, this.start.periodEnd, this.start.notes.trim() || undefined ));
      this.startOpen.set(false);
      await this.router.navigate(['/payroll/runs', detail.run.id]);
    } catch (err) {
      this.startError.set(errorMessage(err));
    } finally {
      this.starting.set(false);
    }
  }
}
