import { CommonModule, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaveAdminService } from '../../../../core/services/leave-admin.service';
import { ENTRY_TYPE_LABELS, LeaveBalance, LeaveBalances, LeaveEntitlementMode, LeaveType, LedgerEntry } from '../../../../shared/models/leave';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Kind = 'grant' | 'adjust';

@Component({
  selector: 'app-employee-leave-balances',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './employee-leave-balances.component.html',
  styleUrl: './employee-leave-balances.component.scss',
})
export class EmployeeLeaveBalancesComponent {
  private admin = inject(LeaveAdminService);

  employeeId = input.required<string>();

  readonly entryLabel = ENTRY_TYPE_LABELS;

  year = signal(new Date().getFullYear());
  balances = signal<LeaveBalances | null>(null);
  types = signal<LeaveType[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  notice = signal<string | null>(null);

  ledgerFor = signal<LeaveBalance | null>(null);
  ledger = signal<LedgerEntry[]>([]);

  postKind = signal<Kind | null>(null);
  post = { leaveTypeId: '', days: 1, reason: '' };
  postError = signal<string | null>(null);
  posting = signal(false);

  constructor() {
    effect(() => { this.employeeId(); void this.load(); });
  }

  async load(): Promise<void> {
    this.loading.set(false);
    try {
      const [balances, types] = await Promise.all([
        firstValueFrom(this.admin.getEmployeeBalances(this.employeeId(), this.year())),
        firstValueFrom(this.admin.getTypes())
      ]);
      this.balances.set(balances);
      this.types.set(types);
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async changeYear(delta: number): Promise<void> {
    this.year.update((y) => y + delta);
    await this.load();
  }

  async openLedger(b: LeaveBalance): Promise<void> {
    this.ledgerFor.set(b);
    this.ledger.set([]);
    try {
      this.ledger.set(await firstValueFrom(this.admin.getEmployeeLedger(this.employeeId(), b.year, b.leaveTypeId)));
    } catch (err) { this.error.set(errorMessage(err)); }
  }

  postableTypes(kind: Kind): LeaveType[] {
    return this.types().filter((t) => 
      kind === 'grant' ? t.entitlementMode === LeaveEntitlementMode.HrGranted
                       : t.entitlementMode !== LeaveEntitlementMode.Unlimited);
  }

  openPost(kind: Kind, typeId = ''): void {
    const options = this.postableTypes(kind);
    const chosen = options.find((t) => t.id === typeId) ?? options[0];
    this.post = { leaveTypeId: chosen?.id ?? '', days: kind === 'grant' ? (chosen?.defaultDays ?? 1) : 1, reason: '' };
    this.postError.set(null);
    this.postKind.set(kind);
  }

  onTypeChange(): void {
    if (this.postKind() !== 'grant') return;
    const t = this.types().find((x) => x.id === this.post.leaveTypeId);
    if (t) this.post.days = t.defaultDays;
  }

  async submit(): Promise<void> {
    const kind = this.postKind();
    if (!kind) return;
    if (!this.post.leaveTypeId) { this.postError.set('Choose a leave type.'); return; }
    if (!this.post.days) { this.postError.set('Enter the number of days.'); return; }
    if (!this.post.reason.trim()) { this.postError.set('A reason is required.'); return; }

    this.posting.set(true);
    this.postError.set(null);
    const value = {
      employeeId: this.employeeId(),
      leaveTypeId: this.post.leaveTypeId,
      year: this.year(),
      days: this.post.days,
      reason: this.post.reason.trim()
    };

    try {
      await firstValueFrom(kind === 'grant' ? this.admin.grant(value) : this.admin.adjust(value));
      this.postKind.set(null);
      this.notice.set(kind === 'grant' ? 'Leave granted.' : 'Balance adjusted.');
      await this.load();
    } catch (err) {
      this.postError.set(errorMessage(err));
    } finally {
      this.posting.set(false);
    }
  }
}
