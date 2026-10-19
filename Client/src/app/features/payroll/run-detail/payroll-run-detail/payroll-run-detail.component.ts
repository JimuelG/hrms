import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PayrollService } from '../../../../core/services/payroll.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { PayrollAdjustment, PayrollLineKind, PayrollResult, PayrollRunDetail, PayrollRunStatus, RUN_STATUS_LABELS, runStattusClass } from '../../../../shared/models/payroll';
import { dateLabel, peso } from '../../../../shared/utils/payroll-ui.util';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Dialog = 'adjust' | 'sendback' | 'cancel' | null;

@Component({
  selector: 'app-payroll-run-detail',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './payroll-run-detail.component.html',
  styleUrl: './payroll-run-detail.component.scss',
})
export class PayrollRunDetailComponent implements OnInit{
  private route = inject(ActivatedRoute);
  private payroll = inject(PayrollService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;

  readonly Status = PayrollRunStatus;
  readonly Kind = PayrollLineKind;
  readonly statusLabel = RUN_STATUS_LABELS;
  readonly statusClass = runStattusClass;
  readonly peso = peso;
  readonly dateLabel = dateLabel;

  private id = this.route.snapshot.paramMap.get('id')!;
  detail = signal<PayrollRunDetail | null>(null);
  adjustments = signal<PayrollAdjustment[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);
  notice = signal<string | null>(null);
  busy = signal(false);
  expanded = signal<ReadonlySet<string>>(new Set());

  dialog = signal<Dialog>(null);
  dialogError = signal<string | null>(null);
  reason = '';
  adj = { employeeId: '', label: '', amount: 0 };


  isDraft = computed(() => this.detail()?.run.status === PayrollRunStatus.Draft);
  withErrors = computed(() => this.detail()?.results.filter((r) => r.errorMessage) ?? []); 

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const d = await firstValueFrom(this.payroll.getRun(this.id));
      this.detail.set(d);
      this.adjustments.set(await firstValueFrom(this.payroll.getAdjustments(this.id)));
      this.loadError.set(null);
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  toggle(r: PayrollResult): void {
    this.expanded.update((s) => 
      { const n = new Set(s); 
        if (!n.delete(r.id)) 
          n.add(r.id); 
        return n; 
      });
  }

  earnings(r: PayrollResult) {
    return r.lines.filter((l) => l.kind === PayrollLineKind.Earning); 
  }

  deductions(r: PayrollResult) {
    return r.lines.filter((l) => l.kind === PayrollLineKind.Deduction);
  }

  adjustmentsFor(employeeId: string) {
    return this.adjustments().filter((a) => a.employeeId === employeeId);
  }

  private async run(action: () => Promise<PayrollRunDetail>, success: string): Promise<boolean> {
    this.busy.set(true);
    this.actionError.set(null);
    try {
      this.detail.set(await action());
      this.adjustments.set(await firstValueFrom(this.payroll.getAdjustments(this.id)));
      this.notice.set(success);
      return true;
    } catch (err) {
      this.actionError.set(errorMessage(err));
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  recompute() {
    return this.run(() => firstValueFrom(this.payroll.recompute(this.id)), 'Recomputed from the latest data.');
  }

  approve() {
    if (!confirm('Approve this run? Someone else will then finalize it.')) return Promise.resolve(false);
    return this.run(() => firstValueFrom(this.payroll.approve(this.id)), 'Run approved.');
  }

  finalize() {
    if (!confirm('Finalize this run? Once finalized it can never be changed.')) {
      return Promise.resolve(false);
    }

    return this.run(() => firstValueFrom(this.payroll.finalize(this.id)), 'Run finalized. It is now locked.');
  }

  openDialog(d: Dialog, employeeId = ''): void {
    this.reason = '';
    this.adj = { employeeId, label: '', amount: 0 };
    this.dialogError.set(null);
    this.dialog.set(d);
  }

  async submitDialog(): Promise<void> {
    const d = this.dialog();
    if (!d) return;
    this.dialogError.set(null);

    if (d === 'adjust') {
      if (!this.adj.employeeId) { this.dialogError.set('Choose an employee.'); return; }
      if (!this.adj.label.trim()) { this.dialogError.set('Give the adjustment a label.'); return; }
      if (!this.adj.amount) { this.dialogError.set('Enter an ammount. Use a negative number for a deduction.'); return; }
    } else if (!this.reason.trim()) {
      this.dialogError.set('A reason is required.');
      return;
    }

    const ok = await this.run(
      d === 'adjust' ? () => firstValueFrom(this.payroll.addAdjustment(this.id, this.adj.employeeId, this.adj.label.trim(), this.adj.amount))
      : d === 'sendback' ? () => firstValueFrom(this.payroll.sendBack(this.id, this.reason.trim()))
      : () => firstValueFrom(this.payroll.cancel(this.id, this.reason.trim())),
      d === 'adjust' ? 'Adjustment added and the run recomputed.' : d === 'sendback' ? 'Send back to draft.' : 'Run cancelled.');

    if (ok) this.dialog.set(null);
    else this.dialogError.set(this.actionError());
  }

  async removeAdjustment(a: PayrollAdjustment): Promise<void> {
    if (!confirm(`Remove "${a.label}"?`)) return;
    await this.run(() => firstValueFrom(this.payroll.removeAdjustment(this.id, a.id)), 'Adjustment removed and the run recomputed/');
  }

}
